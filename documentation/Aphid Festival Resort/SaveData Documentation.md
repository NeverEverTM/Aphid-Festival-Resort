In [SaveSystem] exists a few implementations to properly save and load  in-game data.

## Introduction
These are a base class used to manage the save and load process using json serialization, they include a method for each function plus a way to retroactively apply patches to loaded data for backwards compability.
All classes and interface implementations are within [SaveSystem.cs].

In order to implement a generic C# save data, you need to do the following:
- Create a data holder class, make sure that all serializeable data you need is a public property with public setter and getter, no further requirements are needed.
- Create a class that inherits from IDataModule[T] where [T] is the type of your data holder class, implement the functions Set(), Get() and Default(), where you will respectively; setup data, get the data from its origin, and create a default value for your data class.
-  Create a variable of type SaveModule[T] where [T] is the type of your data holder class. You may also instead create a class that inherits from SaveModule to modify save and load behaviour and then make the variable of that class type.
- Once everything is done, initialize the SaveModule variable, it will ask for a filename/ID string, an instance of your IDataModule inherited class, and optionally, a load priority (higher values means higher priority).
- One final step is to setup a path and extension for the file by enclosing in brackets their default values. Refer to the properties past below.

## PostLoad() & PreLoad()
When loading data from a file, two methods will be called, the first is preload, which fetches the path to the file, and second is postload, which is fed the data read from the file and converts it back to the apropiate data type by deserializing it as a json.

Both of these can be overriden to implement backwards compatibility behaviour such as different file locations, re-formatting incoming json data, or convert the old data type into the new one and passing that.


[DEPRECATED]
SaveModuleGD used to be a class of similar behaviour as above but used Variants as the generic type instead, this has been phased out in favor of just using C#'s internal types

## Customizable Properties
### RootPath: Default = [user://]
The root of the file, by default it goes into Godot's default user directory.
This is changed by a IGlobalCall to the user's profile path.
Ex. "user://profiles/MyResortName"
### RelativePath: Default = [string.Empty]
The relative path from within the root. It does not create folders so make sure that it points to a valid location. A filename is not needed since the assigned ID is used for this. Ex. "root/folder1/folder2"
### Extension: Default = [.json]
A custom extension to use, profile data files tend to use ".data" instead, while global configuration files used ".cfg".