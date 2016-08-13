namespace BucklingSprings.Aware.Core

module DiskSpace = 

    let spaceInGb (directory : string) : uint64 =
        let mutable freeBytesAvailable = 0UL
        let mutable totalNumberOfBytes = 0UL
        let mutable totalNumberOfFreeBytes = 0UL
        if Win32.GetDiskFreeSpaceEx(directory, &freeBytesAvailable, &totalNumberOfBytes, &totalNumberOfFreeBytes) then
            ((freeBytesAvailable / 1024UL) / 1024UL) / 1024UL
        else
            0UL
        

    let available (sizeInGb : int) (directory : string) =
        let available = spaceInGb directory
        available > (uint64 sizeInGb)
        

