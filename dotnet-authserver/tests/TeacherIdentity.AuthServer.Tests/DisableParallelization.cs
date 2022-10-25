namespace TeacherIdentity.AuthServer.Tests;

[CollectionDefinition(nameof(DisableParallelization), DisableParallelization = true)]
public class DisableParallelization : ICollectionFixture<HostFixture> { }
