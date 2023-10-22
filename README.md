# D4MT - Democracy 4 Modding Tool

D4MT is a modding tool for the game Democracy 4, designed to be as user-friendly and intuitive to use as possible.

## Setting up the application

### Pre-requisites

Windows 10 is preferred, as it's the operating system this project was made on. Newer versions of Windows should also work (but isn't guaranteed), if you meet all the other requirements.

Requirements:

- Windows operating system
- Latest Visual Studio 2022
- .NET 7 SDK and runtime

### Installation of pre-requisites

Visual Studio 2022 Community Edition installer can be found from: https://visualstudio.microsoft.com/vs/community/

**The provided installer also contains options for downloading software development kits, such as the .NET 7 SDK and runtime**. *Nevertheless*, you can download .NET 7 from here: https://dotnet.microsoft.com/en-us/download/dotnet/7.0

### Configuring Visual Studio 2022

The included .editorconfig file sets the visual style of code in this solution. If you want to contribute to this repository, then do not modify the .editorconfig file, and do not overwrite it with your own editor's config. All code must follow the same guidelines in this project.

## Running the application

### Debugging the application with Visual Studio 2022

Open the project and while using `Debug` solution configuration and `Any CPU` solution platform press either the debug button or F5.

### Running the release version

TBD

## Running tests

Unit and E2E tests will be added later on, but before minimum viable product release.

## Code standards

Though the code before the full release can look messy and frankly, bad in some cases, this is not meant to be the case forever. This project is set to follow pretty strict code standards, and **adhering to them is expected of every outside contributor**.

1. **Minimal mutation**

    Avoiding mutation is not always possible in C# development, especially UI development, which is why it isn't fully forbidden. But mutation should still be avoided at all costs, when ever possible. Mutation leads to countless amounts of bugs, and frankly, unreadable and unmaintainable code, which is why unnecessary mutation is not allowed. 

    **This rule also concerns collection mutation**. Strive to use only the readonly versions of the collections.

2. **Split large functions into smaller functions**

    Your function should never be longer than about 80 lines. If it is, it's time to split it into multiple smaller, descriptive, pure functions.

3. **Use pure functions**

    Though not always possible, all of your functions should be pure functions, roughly meaning they do not mutate anything outside of their scope, and that they always return the exact same value for the exact same arguments.

4. **No unnecessary comments**

    When code changes, old comments almost always become unnecessary, and usually even misleading. Therefore, you should avoid using comments to document your code. Instead, you should aim to make your code as readable as possible, so it documents itself.

   Exceptions to this rule are **public API documentation** (for example function descriptions visible in IDE) and **@TODO-comments**.

6. **Code self-documenting code**

    Your code has to be readable by maintainers. This means that if you have a function, the maintainer should be able to know what the function does without having to look at the function code itself.

    Your variables also have to be self-documenting.

7. **Avoid magic numbers or strings**

    Every number or string has to have a reason to be there. Therefore, you should either create a descriptive variable for it, and use the variable, or make a constant of it if it's universally used.

    For example, this:

    ```csharp
    int allApples = 6 + 4;
    ```

    should be:

    ```csharp
    int jeremysApples = 6;
    int ronsApples = 4;
    int allApples = jeremysApples + ronsApples;
    ```

8. **Be mindful of for, while, foreach**

    If there's not a good amount of performance to be gained from using for, while, or foreach statements, utilize LINQ functions instead.

9. **Always use newest C# syntax and features available**

    C# is constantly being updated, and you should strive to use the newest available features to their fullest.

10. **Avoid ifs**

    Avoid ifs, especially if they are not used within the context of guard clauses. Always use ternaries for expressions if possible. With **STATEMENTS** you should prefer ifs over ternaries.

11. **Avoid statements**

    Adding to the last rule, avoid using statements. Always use expressions instead.

    For example, instead of this:

    ```csharp
    private int DoX(int x, int y) {
        switch (SomeEnum) {
            case SomeEnum.Two:
                return y;
            case SomeEnum.One:
            default:
                return x;
        }
    }
    ```

    do this:

    ```csharp
    private int DoX(int x, int y) {
        return SomeEnum switch {
            SomeEnum.Two => y,
            SomeEnum.One => x,
            _ => x
        };
    }
    ```

    In some cases, using statements is unavoidable, especially if doing UI code. So don't feel too restricted by this rule.

12. **Use guard clauses**

    Utilize guard clauses whenever possible.

    As a very basic example, instead of this:

    ```csharp
    private int DoX(int x, int y, int z) {
        if (x == z) {
            return x + z;
        } else if (x == y) {
            return x;
        } else {
            return x + y;
        }
    }
    ```

    do this:

    ```csharp
    private int DoX(int x, int y, int z) {
        if (x == z) {
            return x + z;
        }

        if (x == y) {
            return x;
        }

        return x + y;
    }
    ```

13. **Avoid void and try/catch**

    Try to use the TryDoX-pattern whenever applicable. Only use try/catch blocks when appropriate, one appropriate use case would be for example when trying to open a FileStream, since the file could be in use and therefore opening the FileStream would throw an IOException. But even then the catch should be extremely specific, and HRESULT should be utilized whenever possible.

14. **Always test your code before creating a merge request**

    This will become more relevant when unit tests will get added in a future full release.

15. **Don't be afraid to not adhere to some of the code standards when coding**

    *(Within reason)*

    There will be cases where you have to decide between code that adheres to all the code standards, or code that will perform well. In those cases, you should primarily choose the latter - code that will perform well. For example, if you have a case where using mutation would result in an action taking less than 10 seconds where as using immutable code would result in the same action taking more than a minute, you should of course use the mutable method.

## Development guidelines

### Committing and pull requests

You should aspire to have only one commit for one feature, and one pull request for one feature. Use rebasing to achieve this.

To keep the project development unified, you should use the in-built Git tools of Visual Studio 2022.

#### Commit messages

Your commit messages should be short, imperative and clear. For example, instead of:

``Added feature A which only works in case X and did this and that and so on!``

do this:

``Add feature A, B, C, D``

### Adding new dependencies

A simple rule of thumb: do not do it. If you think we need a new dependency then at least consider the following:

- How much do you need to use it?
  - If it's just oneliner in one file then maybe implement the functionality yourself?
- How many dependencies does it have?
  - Pulling in 100 new packages as transitive dependencies for something other than a huge new feature we absolutely
    need to have isn't going to pass the review.

When in doubt [contact me first](mailto:intragalactical@protonmail.com).

## Roadmap
A minimum viable product release is the main priority.<br />
The minimum viable product release will contain *at least* [these features and fixes](https://github.com/Intragalactical/D4MT/milestone/1). <br />
All issues, including those in the minimum viable product release milestone, can be found from [here](https://github.com/Intragalactical/D4MT/issues).
