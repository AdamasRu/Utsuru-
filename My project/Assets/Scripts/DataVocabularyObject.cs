using UnityEngine;
using Utsuru.Capture;

[DisallowMultipleComponent]
public class DataVocabularyObject : InteractableObject
{
    [Tooltip("ID unik kosakata/objek, mis. ICK-02")]
    public new string objectid;

    public override string ObjectId => !string.IsNullOrEmpty(base.ObjectId) ? base.ObjectId : (!string.IsNullOrEmpty(objectid) ? objectid : "");

    private void Awake()
    {
        if (string.IsNullOrEmpty(base.ObjectId) && !string.IsNullOrEmpty(objectid))
        {
            SetObjectId(objectid);
        }
    }
}
