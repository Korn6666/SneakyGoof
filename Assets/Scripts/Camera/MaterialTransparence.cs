// ...existing code...
using System.Collections.Generic;
using UnityEngine;

public class MaterialTransparence : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField, Range(0f, 1f)] private float transparence = 0.5f;
    [SerializeField] private LayerMask obstacleLayer = ~0;
    [SerializeField] private bool debugLogs = false;

    // Renderers actuellement rendus transparents
    private List<Renderer> obstacles = new List<Renderer>();
    // Renderers touchés par le raycast cette frame
    private HashSet<Renderer> newObstacles = new HashSet<Renderer>();

    // Pour restaurer les matériaux originaux
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    // Pour détruire les instances transparentes créées
    private Dictionary<Renderer, Material[]> createdTransparentMaterials = new Dictionary<Renderer, Material[]>();

    void Update()
    {
        if (player == null) return;

        Vector3 dirToPlayer = player.transform.position - transform.position;
        RaycastHit[] hits = Physics.RaycastAll(transform.position, dirToPlayer.normalized, dirToPlayer.magnitude, obstacleLayer);

        newObstacles.Clear();

        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            var go = hit.collider.gameObject;

            if (go.CompareTag("Player")) continue;

            var rend = hit.collider.GetComponent<Renderer>();
            if (rend == null) continue;

            newObstacles.Add(rend);

            if (!obstacles.Contains(rend))
            {
                MakeTransparent(rend, transparence);
                obstacles.Add(rend);
                if (debugLogs) Debug.Log($"Make transparent: {rend.gameObject.name}");
            }
        }

        // Restore those not hit this frame
        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            var rend = obstacles[i];
            if (!newObstacles.Contains(rend))
            {
                RestoreRenderer(rend);
                obstacles.RemoveAt(i);
                if (debugLogs) Debug.Log($"Restore opaque: {rend.gameObject.name}");
            }
        }
    }

    private void MakeTransparent(Renderer rend, float alpha)
    {
        if (rend == null) return;
        if (originalMaterials.ContainsKey(rend)) return; // déjà transparent

        // Stocker les matériaux originaux (shared pour restauration)
        var shared = rend.sharedMaterials;
        originalMaterials[rend] = shared;

        var newMats = new Material[shared.Length];

        for (int i = 0; i < shared.Length; i++)
        {
            var src = shared[i];
            if (src == null)
            {
                newMats[i] = null;
                continue;
            }

            // Créer une instance modifiable
            var inst = new Material(src);
            SetupMaterialForTransparency(inst);
            var col = inst.color;
            col.a = alpha;
            inst.color = col;
            newMats[i] = inst;
        }

        createdTransparentMaterials[rend] = newMats;
        rend.materials = newMats; // applique instances (Renderer.materials clone/create)
    }

    private void RestoreRenderer(Renderer rend)
    {
        if (rend == null) return;
        if (!originalMaterials.ContainsKey(rend)) return;

        // Rétablir les matériaux originaux
        rend.sharedMaterials = originalMaterials[rend];

        // Détruire les instances créées pour éviter fuites
        if (createdTransparentMaterials.TryGetValue(rend, out var created))
        {
            for (int i = 0; i < created.Length; i++)
            {
                if (created[i] != null)
                {
#if UNITY_EDITOR
                    // en mode éditeur : DestroyImmediate
                    DestroyImmediate(created[i]);
#else
                    Destroy(created[i]);
#endif
                }
            }
            createdTransparentMaterials.Remove(rend);
        }

        originalMaterials.Remove(rend);
    }

    // Configure un material (URP shader) pour supporter l'alpha blending
    private void SetupMaterialForTransparency(Material mat)
   {
        if (mat == null) return;

        // 0 = Opaque, 1 = Transparent
        mat.SetFloat("_Surface", 1f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // Blending standard
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        // Activation des bons mots-clés shader
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");

        // Important : désactive le test alpha si actif
        mat.DisableKeyword("_ALPHATEST_ON");
    }

    private void OnDisable()
    {
        // restaurer tout si le component est désactivé
        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            RestoreRenderer(obstacles[i]);
        }
        obstacles.Clear();
    }
}
// ...existing code...