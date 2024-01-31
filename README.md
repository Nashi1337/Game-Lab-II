# GameLab Project Repository

|  General Info  | |
| ---|---|
| Working Title | Wire Generator Plugin |
| Final Title | Wire Generator Plugin |
| Student | Linda Rogner, linda.rogner@stud-mail.uni-wuerzburg.de, s432750 <br> Trucy Petter, trucy.petter@stud-mail.uni-wuerzburg.de, s407385|
| Target Platform(s) | Windows |
| Start Date | 3.11.2022 |
| Study Program | Games Engineering B.Sc.|
| Engine Version | Unity 2020.3.43f1 LTS |

### Abstract

The Wire Generator Plugin for Unity, developed for Electrified by 4hats, lets the user create a wire from one point to another and customize it.
It finds a path between two points and circumvents obstacles with colliders. Each control point of the wire is freely moveable.

## Repository Usage Guides

```
RepositoryRoot/
    ├── README.md           // This should reflect your project
    │                       //  accurately, so always merge infor-
    │                       //  mation from your concept paper
    │                       //  with the readme
    ├── builds/             // Archives (.zip) of built executables of your projects
    │                       //  including (non-standard) dependencies
    ├── code/
    │   ├── engine/         // Place your project folder(s) here
    │   ├── my-game-1/      // No un-used folders, no "archived" folders
    │   ├── CMakeLists.txt  // e.g. if using CMake, this can be your project root
    │   └── ...
    ├── documentation/      // GL2/3 - Each project requires FULL documentation  
    │                       //   i.e. API Docs, Handbook, Dev Docs
    ├── poster/             // PDF of your Poster(s)
    ├── report/             // PDF
    └── trailer/            // .mp4 (final trailer, no raw material)
```

"builds" contains the .unitypackage file that needs to be imported in a Unity project. It contains a demo scene that explains the plugin.
"code" contains the whole code
"documentation" contains a .txt file with a reference to said demo scene. And maybe the same tutorial as PDF if pushing after final works.
"poster" obviously contains the final poster that was hopefully printed for us
"report" contains the report we worked very hard and long on
"trailer" contains a fun little trailer that shows off our cool plugin.
