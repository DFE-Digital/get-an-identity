using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserSearch;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Services.UserImport;

public class UserImportProcessor : IUserImportProcessor
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IUserImportStorageService _userImportStorageService;
    private readonly IUserSearchService _userSearchService;
    private readonly IClock _clock;
    private readonly ILogger<UserImportProcessor> _logger;

    public UserImportProcessor(
        TeacherIdentityServerDbContext dbContext,
        IUserImportStorageService userImportStorageService,
        IUserSearchService userSearchService,
        IClock clock,
        ILogger<UserImportProcessor> logger)
    {
        _dbContext = dbContext;
        _userImportStorageService = userImportStorageService;
        _userSearchService = userSearchService;
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
                else if (row.EmailAddress.Length > User.EmailAddressMaxLength)
                {
                    errors.Add($"{UserImportRow.EmailAddressHeader} field should have a maximum of {User.EmailAddressMaxLength} characters");
                }
                else
                {
                    // Use same email address validation as data annotations attribute
                    var atIndex = row.EmailAddress.IndexOf('@');
                    if (atIndex <= 0 ||
                        atIndex == row.EmailAddress.Length - 1 ||
                        atIndex != row.EmailAddress.LastIndexOf('@'))
                    {
                        errors.Add($"{UserImportRow.EmailAddressHeader} field should be in a valid email address format");
                    }
                }

                if (string.IsNullOrWhiteSpace(row.FirstName))
                {
                    errors.Add($"{UserImportRow.FirstNameHeader} field is empty");
                }
                else if (row.FirstName.Length > User.FirstNameMaxLength)
                {
                    errors.Add($"{UserImportRow.FirstNameHeader} field should have a maximum of {User.FirstNameMaxLength} characters");
                }

                if (string.IsNullOrWhiteSpace(row.LastName))
                {
                    errors.Add($"{UserImportRow.LastNameHeader} field is empty");
                }
                else if (row.LastName.Length > User.LastNameMaxLength)
                {
                    errors.Add($"{UserImportRow.LastNameHeader} field should have a maximum of {User.LastNameMaxLength} characters");
                }

                if (string.IsNullOrWhiteSpace(row.DateOfBirth))
                {
                    errors.Add($"{UserImportRow.DateOfBirthHeader} field is empty");
                }
                else if (!DateOnly.TryParseExact(row.DateOfBirth, "ddMMyyyy", out _))
                {
                    errors.Add($"{UserImportRow.DateOfBirthHeader} field should be a valid date in ddMMyyyy format");
                }

                if (!string.IsNullOrEmpty(row.Trn))
                {
                    if (!Regex.IsMatch(row.Trn, @"^\d{7}$"))
                    {
                        errors.Add($"{UserImportRow.TrnHeader} field must be empty or a 7 digit number");
                    }
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
                var dateOfBirth = DateOnly.ParseExact(row!.DateOfBirth!, "ddMMyyyy", CultureInfo.InvariantCulture);
                // Validate for potential duplicates
                var existingUsers = await _userSearchService.FindUsers(row!.FirstName!, row.LastName!, dateOfBirth);
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
                        FirstName = row.FirstName!,
                        LastName = row.LastName!,
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
                catch (DbUpdateException ex) when (ex.IsUniqueIndexViolation(User.EmailAddressUniqueIndexName))
                {
                    await txn.RollbackAsync();
                    _dbContext.ChangeTracker.Clear();
                    // Refresh the user import job in memory so we can start tracking changes again
                    userImportJob = await _dbContext.UserImportJobs.SingleOrDefaultAsync(j => j.UserImportJobId == userImportJobId);

                    User existingUser = await _dbContext.Users.SingleAsync(u => u.EmailAddress == row!.EmailAddress);
                    if (!string.IsNullOrEmpty(row!.Trn))
                    {
                        if (existingUser.Trn is null)
                        {
                            existingUser.Trn = row.Trn;
                            existingUser.TrnAssociationSource = TrnAssociationSource.UserImport;
                            existingUser.TrnLookupStatus = TrnLookupStatus.Found;
                            userImportJobRow.UserId = existingUser.UserId;
                            userImportJobRow.UserImportRowResult = UserImportRowResult.UserUpdated;
                            userImportJobRow.Notes = new List<string> { "Updated TRN for existing user" };
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
                catch (DbUpdateException ex) when (ex.IsUniqueIndexViolation(User.TrnUniqueIndexName))
                {
                    await txn.RollbackAsync();
                    _dbContext.ChangeTracker.Clear();
                    // Refresh the user import job in memory so we can start tracking changes again
                    userImportJob = await _dbContext.UserImportJobs.SingleOrDefaultAsync(j => j.UserImportJobId == userImportJobId);

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
                catch (DbUpdateException ex) when (ex.IsUniqueIndexViolation("pk_user_import_job_rows"))
                {
                    _logger.LogInformation("Ignoring previously processed row");
                    await txn.RollbackAsync();
                    _dbContext.ChangeTracker.Clear();
                    // Refresh the user import job in memory so we can start tracking changes again
                    userImportJob = await _dbContext.UserImportJobs.SingleOrDefaultAsync(j => j.UserImportJobId == userImportJobId);
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
}
