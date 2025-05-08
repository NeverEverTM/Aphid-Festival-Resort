In [SaveSystem] exists a few implementations to properly save and load  in-game data.

## Introduction
These are a base class used to manage the save and load process using json serialization, they include a method for each function plus a way to retroactively apply patches to loaded data for backwards compability.

While SaveModule[T] is used to serialize generic C# types, one can also use SaveModuleGD, who can store Godot's Variant type variables, useful for when you want to preserve the state of a native GodotObject without deconstructing it to its bare parts or would be otherwise, difficult to piece back together.

All classes and interface implementations are within [SaveSystem.cs].

## SaveModule[T]
In order to implement a generic C# save data, you need to follow a few things:
- Create a data holder class, make sure that all serializeable data you need is a public property with public setter and getter, no further requirements are needed.
- Create a class that inherits from IDataModule[T] where [T] is the type of your data holder class, implement the functions Set(), Get() and Default(), where you will respectively; setup data, get the data from its origin, and create a default value for your data class.
-  Create a variable of type SaveModule[T] where [T] is the type of your data holder class. You may also instead create a class that inherits from SaveModule to modify save and load behaviour and then make the variable of that class type.
- Once everything is done, initialize the SaveModule variable, it will ask for a filename/ID string, an instance of your IDataModule inherited class, and optionally, a load priority (higher values means higher priority).
- One final step is to setup a path and extension for the file by enclosing in brackets their default values. Refer to the properties past below.

## SaveModuleGD
This class works exactly as above, and thus, follows the same procedure, however, it requires a Variant type variable for all its interactions instead of the generic type.
The requested IDataModule must be of type [Variant] in order to be valid.

## Customizable Properties
### RootPath: Default = [user://]
The root of the file, by default it goes into Godot's default user directory.
This is changed by a IGlobalCall to the user's profile path.
Ex. "user://profiles/MyResortName"
### RelativePath: Default = [string.Empty]
The relative path from within the root. It does not create folders so make sure that it points to a valid location. A filename is not needed since the assigned ID is used for this. Ex. "root/folder1/folder2"
### Extension: Default = [.json]
A custom extension to use, profile data files tend to use ".data" instead, while global configuration files used ".cfg".