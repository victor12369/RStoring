# RStoring
A general version-compatible serialization library for .Net.

Based on System.Runtime.Serialization.

In order to solve version-compatible and other problems of System.Runtime.Serialization.

Usage:
```c#
[Storable("R_TEST")]//A unique ID.
public class Test {
    [Stored(0/*Should be unique in one class*/)] int x;
    [Stored(1, Defualt = 123/*If can't find value when deserialize, use Defualt*/)] int y;
    [Stored(2)] int z;
    //If can't find value when deserialize, use this function to construct.
    //With higher priority than Stored.Defualt
    [StoringConstructer(2)]int ConstructZ() { 
        return 321; 
    }
}
```
