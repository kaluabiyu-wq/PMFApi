namespace PmfApi.Tests.Support;

public sealed record TestUser(int Id, string Email, string Role);


public sealed record TestWorld(
    TestUser PatientA,
    TestUser PatientB,
    TestUser SysAdmin,
    TestUser PharmacyAdmin,
    TestUser OwnerA,
    TestUser OwnerB,
    int PharmacyAId,
    int PharmacyBId,
    int PharmacyCId,
    int RxInventoryA,
    int OtcInventoryA,
    int RxInventoryB);
