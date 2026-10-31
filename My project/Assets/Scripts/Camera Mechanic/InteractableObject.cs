using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Utsuru.Capture
{
    /// <summary>
    /// Objek 3D yang bisa dibidik dan difoto. Hanya menyimpan ID unik;
    /// nama dan kosakata diambil dari database lewat ID ini.
    /// Pasang di objek bertag "Interactable" yang punya Collider dan MeshRenderer.
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractableObject : MonoBehaviour
    {
        [Tooltip("ID unik objek, harus sama dengan ID di database.")]
        [SerializeField] private string objectId;

        private readonly List<GameObject> outlineClones = new List<GameObject>();
        private bool outlineBuilt;

        public string ObjectId
        {
            get
            {
                if (!string.IsNullOrEmpty(objectId)) return objectId;
                var vocab = GetComponent<DataVocabularyObject>() ?? GetComponentInParent<DataVocabularyObject>();
                if (vocab != null && !string.IsNullOrEmpty(vocab.objectid)) return vocab.objectid;
                return objectId;
            }
        }

        public void SetObjectId(string id)
        {
            objectId = id;
        }

        /// <summary>Nyalakan/matikan outline. Outline dibangun sekali saat pertama dibutuhkan.</summary>
        public void SetHighlighted(bool on, Material outlineMaterial)
        {
            if (on && !outlineBuilt) BuildOutline(outlineMaterial);

            for (int i = 0; i < outlineClones.Count; i++)
            {
                if (outlineClones[i] != null) outlineClones[i].SetActive(on);
            }
        }

        // Teknik "inverted hull": salinan mesh dengan material outline (Cull Front).
        private void BuildOutline(Material outlineMaterial)
        {
            outlineBuilt = true;
            if (outlineMaterial == null) return;

            foreach (var meshRenderer in GetComponentsInChildren<MeshRenderer>())
            {
                var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null) continue;

                var clone = new GameObject("__Outline");
                clone.transform.SetParent(meshRenderer.transform, false);
                clone.layer = 2; // Ignore Raycast, supaya tidak mengganggu deteksi

                clone.AddComponent<MeshFilter>().sharedMesh = meshFilter.sharedMesh;

                var cloneRenderer = clone.AddComponent<MeshRenderer>();
                var materials = new Material[meshFilter.sharedMesh.subMeshCount];
                for (int i = 0; i < materials.Length; i++) materials[i] = outlineMaterial;
                cloneRenderer.sharedMaterials = materials;
                cloneRenderer.shadowCastingMode = ShadowCastingMode.Off;
                cloneRenderer.receiveShadows = false;

                clone.SetActive(false);
                outlineClones.Add(clone);
            }
        }
    }
}
