using UnityEngine;

public class Refs
{
    private static ReferencesSO _cache;
    public static ReferencesSO R
    {
        get
        {
            if (_cache == null)
            {
                _cache = Resources.Load<ReferencesSO>("ReferencesSO");
            }
            return _cache;
        }
    }
}