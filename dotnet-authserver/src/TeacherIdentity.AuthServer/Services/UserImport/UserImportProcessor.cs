using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;
using Polly;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.Http;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.UserSearch;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Services.UserImport;

public class UserImportProcessor : IUserImportProcessor
{
    private const int DqtApiRetryCount = 3;

    private readonly TimeSpan DqtApiDefaultRetryAfter = TimeSpan.FromSeconds(30);
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IUserImportStorageService _userImportStorageService;
    private readonly IUserSearchService _userSearchService;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly IClock _clock;
    private readonly ILogger<UserImportProcessor> _logger;

    public UserImportProcessor(
        TeacherIdentityServerDbContext dbContext,
        IUserImportStorageService userImportStorageService,
        IUserSearchService userSearchService,
        IDqtApiClient dqtApiClient,
        IClock clock,
        ILogger<UserImportProcessor> logger)
    {
        _dbContext = dbContext;
        _userImportStorageService = userImportStorageService;
        _userSearchService = userSearchService;
        _dqtApiClient = dqtApiClient;
        _clock = clock;
        _logger = logger;
    }

    public async Task Process(Guid userImportJobId)
    {
        var sw = Stopwatch.StartNew();

        var userImportJob = await _dbContext.UserImportJobs.SingleOrDefaultAsync(j => j.UserImportJobId == userImportJobId);
        if (userImportJob == null)
        {
            return;
        }

        using var stream = await _userImportStorageService.OpenReadStream(userImportJob.StoredFilename);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });
        csv!.Read();
        csv.ReadHeader();
        int rowNumber = 0;
        while (csv.Read())
        {
            rowNumber++;
            var errors = new List<string>();
            UserImportRow? row = null;
            User? user = null;
            string? id = null;
            TeacherInfo? dqtTeacher = null;
            string? firstName = null;
            string? middleName = null;
            string? lastName = null;

            // Check we don't have wonky rows i.e. too few or too many fields
            if (!csv.TryGetField<string>(UserImportRow.ColumnCount - 1, out _))
            {
                errors.Add($"Exactly {UserImportRow.ColumnCount} fields expected (this row has less)");
            }
            else if (csv.TryGetField<string>(UserImportRow.ColumnCount, out _))
            {
                errors.Add($"Exactly {UserImportRow.ColumnCount} fields expected (this row has more)");
            }
            else
            {
                row = csv.GetRecord<UserImportRow>();
                if (string.IsNullOrWhiteSpace(row!.Id))
                {
                    errors.Add($"{UserImportRow.IdHeader} field is empty");
                }
                else if (row.Id.Length > UserImportJobRow.IdMaxLength)
                {
                    errors.Add($"{UserImportRow.IdHeader} field should have a maximum of {UserImportJobRow.IdMaxLength} characters");
                }
                else
                {
                    id = row.Id;
                }

                if (string.IsNullOrWhiteSpace(row.EmailAddress))
                {
                    errors.Add($"{UserImportRow.EmailAddressHeader} field is empty");
                }
                else if (row.EmailAddress.Length > EmailAddress.EmailAddressMaxLength)
                {
                    errors.Add($"{UserImportRow.EmailAddressHeader} field should have a maximum of {EmailAddress.EmailAddressMaxLength} characters");
                }
                else
                {
                    // Use same email address validation as Notify
                    if (!EmailAddress.TryParse(row.EmailAddress, out _))
                    {
                        errors.Add($"{UserImportRow.EmailAddressHeader} field should be in a valid email address format");
                    }
                }

                if (!string.IsNullOrEmpty(row.Trn))
                {
                    if (!Regex.IsMatch(row.Trn, @"^\d{7}$"))
                    {
                        errors.Add($"{UserImportRow.TrnHeader} field must be empty or a 7 digit number");
                    }
                    else
                    {
                        dqtTeacher = await Policy
                            .Handle<TooManyRequestsException>()
                            .WaitAndRetryAsync(
                                DqtApiRetryCount,
                                sleepDurationProvider: (retryCount, exception, context) =>
                                {
                                    var rateLimitingException = exception as TooManyRequestsException;
                                    return rateLimitingException!.RetryAfter ?? DqtApiDefaultRetryAfter;
                                },
                                onRetryAsync: (exception, delay, retryCount, context) =>
                                {
                                    _logger.LogInformation($"Executing retry number {retryCount} for rate-limited call to DQT API");
                                    return Task.CompletedTask;
                                })
                            .ExecuteAsync(() => _dqtApiClient.GetTeacherByTrn(row.Trn));

                        if (dqtTeacher is null)
                        {
                            errors.Add($"{UserImportRow.TrnHeader} field must match a record in DQT");
                        }
                    }
                }

                if (string.IsNullOrEmpty(row.Trn))
                {
                    if (string.IsNullOrWhiteSpace(row.FirstName))
                    {
                        errors.Add($"{UserImportRow.FirstNameHeader} field is empty");
                    }
                    else if (row.FirstName.Length > User.FirstNameMaxLength)
                    {
                        errors.Add($"{UserImportRow.FirstNameHeader} field should have a maximum of {User.FirstNameMaxLength} characters");
                    }

                    if (!string.IsNullOrEmpty(row.MiddleName) && row.MiddleName.Length > User.MiddleNameMaxLength)
                    {
                        errors.Add($"{UserImportRow.MiddleNameHeader} field should have a maximum of {User.MiddleNameMaxLength} characters");
                    }

                    if (string.IsNullOrWhiteSpace(row.LastName))
                    {
                        errors.Add($"{UserImportRow.LastNameHeader} field is empty");
                    }
                    else if (row.LastName.Length > User.LastNameMaxLength)
                    {
                        errors.Add($"{UserImportRow.LastNameHeader} field should have a maximum of {User.LastNameMaxLength} characters");
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(row.FirstName))
                    {
                        errors.Add($"{UserImportRow.FirstNameHeader} field must be empty when {UserImportRow.TrnHeader} field is specified");
                    }

                    if (!string.IsNullOrWhiteSpace(row.MiddleName))
                    {
                        errors.Add($"{UserImportRow.MiddleNameHeader} field must be empty when {UserImportRow.TrnHeader} field is specified");
                    }

                    if (!string.IsNullOrWhiteSpace(row.LastName))
                    {
                        errors.Add($"{UserImportRow.LastNameHeader} field must be empty when {UserImportRow.TrnHeader} field is specified");
                    }
                }

                if (!string.IsNullOrEmpty(row.PreferredName) && row.PreferredName.Length > User.PreferredNameMaxLength)
                {
                    errors.Add($"{UserImportRow.PreferredNameHeader} field should have a maximum of {User.PreferredNameMaxLength} characters");
                }

                if (string.IsNullOrWhiteSpace(row.DateOfBirth))
                {
                    errors.Add($"{UserImportRow.DateOfBirthHeader} field is empty");
                }
                else if (!DateOnly.TryParseExact(row.DateOfBirth, "ddMMyyyy", out _))
                {
                    errors.Add($"{UserImportRow.DateOfBirthHeader} field should be a valid date in ddMMyyyy format");
                }
            }

            var userImportJobRow = new UserImportJobRow
            {
                UserImportJobId = userImportJobId,
                RowNumber = rowNumber,
                Id = id,
                RawData = csv.Parser.RawRecord.TrimEnd('\r', '\n')
            };

            if (errors.Count > 0)
            {
                userImportJobRow.Notes = errors;
                userImportJobRow.UserImportRowResult = UserImportRowResult.Invalid;
            }
            else
            {
                firstName = dqtTeacher != null ? dqtTeacher.FirstName : row!.FirstName!;
                middleName = dqtTeacher != null ? dqtTeacher.MiddleName : row!.MiddleName;
                lastName = dqtTeacher != null ? dqtTeacher.LastName : row!.LastName!;

                var dateOfBirth = DateOnly.ParseExact(row!.DateOfBirth!, "ddMMyyyy", CultureInfo.InvariantCulture);
                // Validate for potential duplicates
                var existingUsers = await _userSearchService.FindUsers(firstName, lastName, dateOfBirth);
                if (existingUsers.Any(u => u.EmailAddress != row.EmailAddress!))
                {
                    errors.Add("Potential duplicate user");
                    userImportJobRow.Notes = errors;
                    userImportJobRow.UserImportRowResult = UserImportRowResult.Invalid;
                }
                else
                {
                    user = new User
                    {
                        UserId = Guid.NewGuid(),
                        EmailAddress = row.EmailAddress!,
                        FirstName = firstName,
                        MiddleName = middleName,
                        LastName = lastName,
                        PreferredName = row.PreferredName,
                        Created = _clock.UtcNow,
                        Updated = _clock.UtcNow,
                        DateOfBirth = dateOfBirth,
                        UserType = UserType.Teacher
                    };

                    if (!string.IsNullOrEmpty(row.Trn))
                    {
                        user.Trn = row.Trn;
                        user.TrnAssociationSource = TrnAssociationSource.UserImport;
                        user.TrnLookupStatus = TrnLookupStatus.Found;
                    }

                    userImportJobRow.UserId = user.UserId;
                    userImportJobRow.UserImportRowResult = UserImportRowResult.UserAdded;
                }
            }

            using (var txn = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (user != null)
                    {
                        _dbContext.Users.Add(user);
                        _dbContext.AddEvent(new UserImportedEvent
                        {
                            UserImportJobId = userImportJobId,
                            CreatedUtc = _clock.UtcNow,
                            User = Events.User.FromModel(user)
                        });
                    }

                    if (userImportJob!.UserImportJobRows == null)
                    {
                        userImportJob.UserImportJobRows = new List<UserImportJobRow>();
                    }

                    userImportJob.UserImportJobRows.Add(userImportJobRow);

                    await _dbContext.SaveChangesAsync();
                    await txn.CommitAsync();
                }
                catch (UniqueConstraintException ex)
                {
                    await txn.RollbackAsync();
                    _dbContext.ChangeTracker.Clear();
                    // Refresh the user import job in memory so we can start tracking changes again
                    userImportJob = await _dbContext.UserImportJobs.SingleOrDefaultAsync(j => j.UserImportJobId == userImportJobId);

                    if (ex.IsUniqueIndexViolation("pk_user_import_job_rows"))
                    {
                        _logger.LogInformation("Ignoring previously processed row");
                        return;
                    }

                    if (ex.IsUniqueIndexViolation(User.EmailAddressUniqueIndexName))
                    {
                        await HandleEmailAddressUniqueIndexViolation(row, userImportJobRow, userImportJob, dqtTeacher);
                        return;
                    }

                    if (ex.IsUniqueIndexViolation(User.TrnUniqueIndexName))
                    {
                        await HandleTrnUniqueIndexViolation(row, userImportJobRow, userImportJob);
                        return;
                    }
                }
            }
        }

        userImportJob!.UserImportJobStatus = UserImportJobStatus.Processed;
        userImportJob.Imported = _clock.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _userImportStorageService.Archive(userImportJob.StoredFilename);

        sw.Stop();
        _logger.LogInformation($"Processed {rowNumber} user import rows in {sw.ElapsedMilliseconds}ms.");
    }

    private async Task HandleEmailAddressUniqueIndexViolation(
        UserImportRow? row,
        UserImportJobRow userImportJobRow,
        UserImportJob? userImportJob,
        TeacherInfo? dqtTeacher)
    {
        User existingUser = await _dbContext.Users.SingleAsync(u => u.EmailAddress == row!.EmailAddress);

        if (!string.IsNullOrEmpty(row!.Trn))
        {
            if (existingUser.Trn is null)
            {
                // First double-check that the TRN we are going to assign to this user is not already in use
                var existingUsersWithTrn = await _dbContext.Users.CountAsync(u => u.Trn == row!.Trn);
                if (existingUsersWithTrn > 0)
                {
                    userImportJobRow.UserId = null;
                    userImportJobRow.UserImportRowResult = UserImportRowResult.Invalid;
                    userImportJobRow.Notes = new List<string> { "A user already exists with the specified TRN but a different email address" };
                }
                else
                {
                    var changes = UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus |
                        (existingUser.FirstName != dqtTeacher!.FirstName ? UserUpdatedEventChanges.FirstName : UserUpdatedEventChanges.None) |
                        (existingUser.MiddleName != dqtTeacher!.MiddleName ? UserUpdatedEventChanges.MiddleName : UserUpdatedEventChanges.None) |
                        (existingUser.LastName != dqtTeacher.LastName ? UserUpdatedEventChanges.LastName : UserUpdatedEventChanges.None);

                    existingUser.Trn = row.Trn;
                    existingUser.TrnAssociationSource = TrnAssociationSource.UserImport;
                    existingUser.TrnLookupStatus = TrnLookupStatus.Found;
                    existingUser.FirstName = dqtTeacher.FirstName;
                    existingUser.MiddleName = dqtTeacher.MiddleName;
                    existingUser.LastName = dqtTeacher.LastName;

                    userImportJobRow.UserId = existingUser.UserId;
                    userImportJobRow.UserImportRowResult = UserImportRowResult.UserUpdated;
                    userImportJobRow.Notes = new List<string> { "Updated TRN and name for existing user" };

                    _dbContext.AddEvent(new UserUpdatedEvent()
                    {
                        Source = UserUpdatedEventSource.UserImport,
                        UpdatedByClientId = null,
                        UpdatedByUserId = userImportJob!.UploadedByUserId,
                        CreatedUtc = _clock.UtcNow,
                        User = Events.User.FromModel(existingUser),
                        Changes = changes
                    });
                }
            }
            else if (existingUser.Trn != row.Trn)
            {
                userImportJobRow.UserId = null;
                userImportJobRow.UserImportRowResult = UserImportRowResult.Invalid;
                userImportJobRow.Notes = new List<string> { "A user already exists with the specified email address but a different TRN" };
            }
            else
            {
                userImportJobRow.UserImportRowResult = UserImportRowResult.None;
                userImportJobRow.Notes = new List<string> { "A user already exists with the specified email address" };
            }
        }
        else
        {
            userImportJobRow.UserImportRowResult = UserImportRowResult.None;
            userImportJobRow.Notes = new List<string> { "A user already exists with the specified email address" };
        }

        if (userImportJob!.UserImportJobRows == null)
        {
            userImportJob.UserImportJobRows = new List<UserImportJobRow>();
        }
        userImportJob.UserImportJobRows.Add(userImportJobRow);

        await _dbContext.SaveChangesAsync();
    }

    private async Task HandleTrnUniqueIndexViolation(
        UserImportRow? row,
        UserImportJobRow userImportJobRow,
        UserImportJob? userImportJob)
    {
        User existingUser = await _dbContext.Users.SingleAsync(u => u.Trn == row!.Trn);

        if (existingUser.EmailAddress == row!.EmailAddress)
        {
            userImportJobRow.UserId = existingUser.UserId;
            userImportJobRow.UserImportRowResult = UserImportRowResult.None;
            userImportJobRow.Notes = new List<string> { "A user already exists with the specified email address" };
        }
        else
        {
            userImportJobRow.UserId = null;
            userImportJobRow.UserImportRowResult = UserImportRowResult.Invalid;
            userImportJobRow.Notes = new List<string> { "A user already exists with the specified TRN" };
        }

        if (userImportJob!.UserImportJobRows == null)
        {
            userImportJob.UserImportJobRows = new List<UserImportJobRow>();
        }
        userImportJob.UserImportJobRows.Add(userImportJobRow);

        await _dbContext.SaveChangesAsync();
    }
}
