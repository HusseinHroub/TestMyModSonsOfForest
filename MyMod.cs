using Sons.Ai.Vail;
using Sons.Inventory;
using SonsSdk;
using System.Drawing;
using System;
using UnityEngine;

namespace MyMod;

public class MyMod : SonsMod
{
    private DebugTools.LineDrawer aimLine;
    public MyMod()
    {

        // Uncomment any of these if you need a method to run on a specific update loop.
        OnUpdateCallback = MyUpdateMethod;
        //OnLateUpdateCallback = MyLateUpdateMethod;
        //OnFixedUpdateCallback = MyFixedUpdateMethod;
        //OnGUICallback = MyGUIMethod;

        // Uncomment this to automatically apply harmony patches in your assembly.
        //HarmonyPatchAll = true;

    }

    protected override void OnInitializeMod()
    {
        // Do your early mod initialization which doesn't involve game or sdk references here
        Config.Init();
        Log("Config initialized successfully!");
    }

    protected override void OnSdkInitialized()
    {
        // Do your mod initialization which involves game or sdk references here
        // This is for stuff like UI creation, event registration etc.
        MyModUi.Create();

        // Add in-game settings ui for your mod.
        // SettingsRegistry.CreateSettings(this, null, typeof(Config));
        Log("Mod fully initialized and ready!", System.Drawing.Color.Green);
        GlobalInput.RegisterKey(KeyCode.E, CheckWhatImLookingAt);

        // Do not eagerly assume internal LineDrawer runtime state is ready here.
        // We'll lazily create it when first used.
    }

    private void MyUpdateMethod()
    {
        Camera camera = Camera.main;
        if (camera == null) return;

        // Cast ray from center of screen
        Ray ray = camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        Vector3 startPos = ray.origin;
        Vector3 endPos;

        float maxDistance = 10f;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            // Hit something - line ends at hit point
            endPos = hit.point;
        }
        else
        {
            // Didn't hit anything - line extends full distance
            endPos = ray.origin + ray.direction * maxDistance;
        }

        // Lazily create the LineDrawer and guard SetLine to avoid crashes if its internal Transform isn't ready.
        if (aimLine == null)
        {
            try
            {
                aimLine = new DebugTools.LineDrawer();
            }
            catch (Exception ex)
            {
                Log($"Failed to create LineDrawer: {ex.Message}");
                return;
            }
        }

        try
        {
            aimLine.SetLine(startPos, endPos);
            aimLine.Active = true;
        }
        catch (NullReferenceException nre)
        {
            // Log details so you can inspect DebugTools.LineDrawer implementation
            Log($"LineDrawer.SetLine threw NullReferenceException: {nre.Message}");
            // Optionally deactivate to avoid repeated exceptions
            if (aimLine != null) aimLine.Active = false;
        }
        catch (Exception ex)
        {
            Log($"LineDrawer.SetLine threw: {ex.Message}");
        }
    }

    protected override void OnGameStart()
    {
        // This is called once the player spawns in the world and gains control.
        Log("Player is now in the world and has control!");
    }

    private void CheckWhatImLookingAt()
    {
        Log("Checking what I am looking at!");
        // Get the camera (player's viewpoint)
        Camera camera = Camera.main; // or find player's camera
        if (camera == null)
        {
            Log("Camera not found!");
            return;
        }

        // Cast a ray from camera forward
        Ray ray = camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        float maxDistance = 10f; // How far to check

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            Log($"Hit: {hit.collider.gameObject.name}");
            Log($"Distance: {hit.distance}");
            Log($"Position: {hit.point}");

            // Check if it's an item
            var itemComponent = hit.collider.GetComponent<ItemInstance>();
            if (itemComponent != null)
            {
                Log("It's an item!");
            }

            // Check if it's an actor
            var actor = hit.collider.GetComponent<VailActor>();
            if (actor != null)
            {
                Log($"It's an actor: {actor.name}");
            }

            SonsTools.ShowMessage($"Looking at: {hit.collider.gameObject.name}", 2f);
        }
        else
        {
            Log("Not looking at anything");
        }
    }
}