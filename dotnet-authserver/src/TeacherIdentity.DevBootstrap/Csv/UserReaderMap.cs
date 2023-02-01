using CsvHelper.Configuration;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.DevBootstrap.Csv;

internal class UserReaderMap : ClassMap<User>
{
    public UserReaderMap()
    {
        Map(i => i.UserId).Index(0);
        Map(i => i.EmailAddress).Index(1);
        Map(i => i.FirstName).Index(2);
        Map(i => i.LastName).Index(3);
        Map(i => i.DateOfBirth).Index(4).TypeConverterOption.Format("ddMMyyyy");
        Map(i => i.UserType).Constant(UserType.Teacher);
        Map(i => i.Trn).Index(6);
        Map(i => i.Created).Convert(row => DateTime.UtcNow);
        Map(i => i.Updated).Convert(row => DateTime.UtcNow);
    }
}
