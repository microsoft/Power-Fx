namespace Microsoft.PowerFx.Core.Localization
{
    
    /// <summary>
    /// This interface is used by Canvas apps to pass in an interface to access strings for keys that are resolved later by PowerFx.
    /// Ideally it would be removed, but separating this is tricky, and this allows PowerFx to stand on its own. 
    /// </summary>
    internal interface IExternalStringResources
    {
        bool TryGetErrorResource(ErrorResourceKey resourceKey, out ErrorResource resourceValue, string locale = null);
        bool TryGet(string resourceKey, out string resourceValue, string locale = null);
    }
}
