using Sons.Ai.Vail;
using Sons.Inventory;
using SonsSdk;
using TheForest.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using Color = System.Drawing.Color;

namespace MyMod;

public class MyMod : SonsMod
{
    private DebugTools.LineDrawer aimLine;
    private RaycastHit? _raycastHit;
    private int playerLayerMask;

    public MyMod()
    {
        playerLayerMask = (1 << 0) //default
                          // | (1 << 3)  // BasicTrigger
                          // | (1 << 15) // Held
                          // | (1 << 16) // Ragdoll
                          // | (1 << 17) // Pusher
                          | (1 << 18) // Player
                          | (1 << 19) // Character
                          // | (1 << 22) // BasicCollider
                          | (1 << 23) // Inventory
                          // | (1 << 24) // Poke
                          | (1 << 27) // Camera
                          // | (1 << 28) // PickUp
                          | (1 << 29); // Grab
        // Use GUI callback as requested
        OnLateUpdateCallback = MyGUIMethod;
    }

    protected override void OnInitializeMod()
    {
        Config.Init();
        Log("Config initialized successfully!");
    }

    protected override void OnSdkInitialized()
    {
        MyModUi.Create();
        Log("Mod fully initialized and ready!", Color.Green);
        GlobalInput.RegisterKey(KeyCode.E, CheckWhatImLookingAt);
    }

    protected override void OnGameStart()
    {
        // Create the library LineDrawer when the scene and item DB are ready.
        try
        {
            aimLine = new DebugTools.LineDrawer();
            if (aimLine != null && aimLine.LineRenderer != null)
            {
                var lr = aimLine.LineRenderer;
                lr.startWidth = 0.04f;
                lr.endWidth = 0.04f;
                lr.startColor = new UnityEngine.Color(1f, 0f, 0f, 1f); // solid red
                lr.endColor = new UnityEngine.Color(1f, 0f, 0f, 1f); // solid red
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.alignment = LineAlignment.View; // line always faces camera
                aimLine.Active = true;
                Log("Library LineDrawer created and configured.");
            }
            else
            {
                aimLine = null;
                Log("Library LineDrawer creation returned null or missing LineRenderer.");
            }
        }
        catch (Exception ex)
        {
            aimLine = null;
            Log($"LineDrawer creation failed in OnGameStart: {ex.Message}");
        }

        Log("Player is now in the world and has control!");
    }

    private void MyGUIMethod()
    {
        if (!LocalPlayer.IsInWorld)
        {
            return;
        }

        var t = LocalPlayer.MainCamTr;


        RaycastHit hit;

        Vector3 startPos = t.position + t.forward * 1f;
        startPos += t.up * -0.1f;
        startPos += t.right * 0.1f;

        Ray ray = new Ray(startPos, t.forward); // <-- ray now starts at the offset
        Vector3 endPos;
        float maxDistance = 100f;
        var lr = aimLine.LineRenderer;
        if (Physics.Raycast(ray, out hit, maxDistance /*, ~playerLayerMask*/))
        {
            _raycastHit = hit;
            endPos = hit.point;
            lr.startColor = new UnityEngine.Color(0f, 1f, 0f, 1f); // solid red
            lr.endColor = new UnityEngine.Color(0f, 1f, 0f, 1f); // solid red
        }
        else
        {
            _raycastHit = null;
            endPos = ray.origin + ray.direction * maxDistance;
            lr.startColor = new UnityEngine.Color(1f, 0f, 0f, 1f); // solid red
            lr.endColor = new UnityEngine.Color(1f, 0f, 0f, 1f); // solid red
        }

        if (aimLine == null || aimLine.LineRenderer == null)
            return;

        try
        {
            // IMPORTANT: update the LineRenderer positions directly (world-space),
            // do NOT call aimLine.SetLine/SetPosition which moves the Transform each frame.
            aimLine.SetLine(startPos, endPos);
            lr.enabled = true;
        }
        catch (Exception ex)
        {
            Log($"Updating library LineRenderer failed: {ex.Message}");
            try
            {
                aimLine.Active = false;
            }
            catch
            {
            }
        }
    }

    private void CheckWhatImLookingAt()
    {
//         var playerColliders = LocalPlayer.GameObject.GetComponentsInChildren<Collider>(true);
//         HashSet<int> layers = new HashSet<int>();
//
//         foreach (var col in playerColliders)
//         {
//             layers.Add(col.gameObject.layer);
//         }
//
// // Print layers
//         foreach (var l in layers)
//         {
//             Log($"LocalPlayer has collider on layer {l} ({LayerMask.LayerToName(l)})");
//         }


        if (_raycastHit == null)
        {
            return;
        }

        Log($"Hit: {_raycastHit?.collider.gameObject}");
        // Log($"Distance: {_raycastHit?.distance}");
        // Log($"Position: {_raycastHit?.point}");


        Transform current = _raycastHit?.collider.transform;
        Transform last = current;
        while (current.parent != null)
        {
            current = current.parent;
            last = current; // keep track of last non-null
        }
        
        Log($"Hit {last.gameObject.name} on layer {last.gameObject.layer} ({LayerMask.LayerToName(last.gameObject.layer)})");

        Log(last.gameObject.name);

        var itemComponent = _raycastHit?.collider.GetComponent<ItemInstance>();
        if (itemComponent != null) Log("It's an item!");

        var actor = last.GetComponent<VailActor>();
        if (actor != null)
        {
            Log($"It's an actor: {actor.name}");
            // var dir = LocalPlayer.MainCamTr.forward * -1f * SonsTools.GetPlayerDistance(actor.transform.position);
            // dir += Vector3.up * 5f;
            
            
            
            var toPlayer = (LocalPlayer.Transform.position - actor.transform.position);
            // float distance = toPlayer.magnitude;
            // Vector3 dir = toPlayer.normalized * distance * 0.5f; // tweak the 0.5f as neede
            
            toPlayer += Vector3.up * 5f;
            actor.AddKnockUpVelocity(toPlayer);
        }

        SonsTools.ShowMessage($"Looking at: {last.gameObject.name}", 2f);

        Rigidbody rb = last.gameObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Log("rigid body attached!");
            Vector3 force = new Vector3(0f, 40f, 0f); // X direction
            rb.AddForce(force, ForceMode.Impulse); // Apply immediately
        }
    }
}