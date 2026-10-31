using UnityEngine;
using Utsuru.Capture;

[DisallowMultipleComponent]
public class DataVocabularyObject : InteractableObject
{
    [Tooltip("ID unik kosakata/objek, mis. ICK-02")]
    public string objectid;

    private void Awake()
    {
        if (string.IsNullOrEmpty(ObjectId) && !string.IsNullOrEmpty(objectid))
        {
            SetObjectId(objectid);
        }
    }
}
