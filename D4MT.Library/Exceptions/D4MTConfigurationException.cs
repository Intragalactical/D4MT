namespace D4MT.Library.Exceptions;

public sealed class D4MTConfigurationException(string message) : Exception(message) {
    public static readonly D4MTConfigurationException CouldNotDeserialize = new("Could not deserialize configuration!");
}
