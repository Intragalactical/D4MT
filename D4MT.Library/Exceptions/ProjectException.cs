namespace D4MT.Library.Exceptions;

public sealed class ProjectException(string message) : Exception(message) {
    public static readonly ProjectException CouldNotDeserialize = new("Could not deserialize project!");
    public static readonly ProjectException CouldNotGetProjectDirectory = new("Could not parse project directory from file path!");
}
