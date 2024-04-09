using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static SceneManager;

public static class SceneManager
{

    private static Control mainScene;
    private static Control backSceneParent;
    private static ColorRect backSceneDimmer;
    private static Control frontSceneParent;

    public enum Scene
    {
        None,
        MainMenu,
        PreGameUI,
        InGameUI,
        PostGameUI,
        ConnectionProgress,
        ConnectionLost,
        PauseMenu,
        Settings,
        HostSettings
    }

    private static Dictionary<Scene, string> scenePaths = new Dictionary<Scene, string>()
    {
        {Scene.MainMenu, "res://SceneAssets/MainMenuUI/MainMenuUI.tscn" },
        {Scene.PreGameUI, "res://SceneAssets/PreGameUI/PreGameUI.tscn" },
        {Scene.InGameUI, "res://SceneAssets/InGameUI/InGameUI.tscn" },
        {Scene.ConnectionProgress, "res://SceneAssets/ConnectionProgressUI/ConnectionProgressUI.tscn" },
        {Scene.PauseMenu, "res://SceneAssets/PauseMenuUI/PauseMenuUI.tscn" },
        {Scene.PostGameUI,"res://SceneAssets/PostGameUI/PostGameUI.tscn"},
        {Scene.ConnectionLost, "res://SceneAssets/DisconnectUI/DisconnectUI.tscn" },
    };

    public static Scene frontScene { get; private set; } = Scene.None;
    public static Scene backScene { get; private set; } = Scene.None;

    private static Dictionary<Scene, PackedScene> packedScenes = new Dictionary<Scene, PackedScene>() { };

    public static void SetMainScene(Control scene)
    {
        mainScene = scene;
        backSceneParent = mainScene.GetNode<Control>("%BackScene");
        backSceneDimmer = mainScene.GetNode<ColorRect>("%BackSceneDimmer");
        frontSceneParent = mainScene.GetNode<Control>("%FrontScene");

        SetFrontScene(frontScene);
        SetBackScene(backScene);

    }

    private static Node InstantiateScene(Scene scene)
    {
        string scenePath;
        if(!scenePaths.TryGetValue(scene, out scenePath))
        {
            return null;
        }

        PackedScene packedScene;
        if(!packedScenes.TryGetValue(scene, out packedScene))
        {
            packedScene = ResourceLoader.Load<PackedScene>(scenePath);
            packedScenes[scene] = packedScene;
        }

        return packedScene.Instantiate<Node>();
    }


    public static void SetFrontScene(Scene scene)
    {
        frontScene = scene;
        if(mainScene == null) { return; }

        foreach(Node child in frontSceneParent.GetChildren())
        {
            child.QueueFree();
        }

        Node node = InstantiateScene(scene);
        if(node != null)
        {
            frontSceneParent.AddChild(node);
            backSceneDimmer.Visible = true;
        }
        else
        {
            backSceneDimmer.Visible = false;
        }
    }
    public static void SetBackScene(Scene scene)
    {
        backScene = scene;
        if (mainScene == null) { return; }

        foreach (Node child in backSceneParent.GetChildren())
        {
            child.QueueFree();
        }

        Node node = InstantiateScene(scene);
        if (node != null)
        {
            backSceneParent.AddChild(node);
        }
    }
    public static void ClearFrontScene()
    {
        frontScene = Scene.None;
        if (mainScene == null) { return; }

        foreach (Node child in frontSceneParent.GetChildren())
        {
            child.QueueFree();
        }

        backSceneDimmer.Visible = false;
    }
}