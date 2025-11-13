using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InCubeLibrary
{
    public enum SynchResults
    {
        Success = 1,
        DevicesNotLicenced = 2,
        UserNameOrPassNotValid = 3,
        NoRoute = 4,
        DeviceNotAssignedForEmployee = 5,
        ConnectionError = 6,
        ErrorCompressFile = 7,
        ErrorPreparingData = 8,
        CannotOpenDataBase = 9,
        EmployeeOnHold = 10,
        EmployeeInactive = 11,
        EmployeeOperatorNotExists = 12,
        NoEmployeeTerritory = 13,
        NoEmployeeVehicle = 14,
        NoEmployeeRouteForThisDay = 15,
        ErrorPreparingMasterData = 16,
        ErrorCreateNewRouteHistory = 17,
        SDFLocked = 18,
        EmptySDFNotFound = 19
    }
}