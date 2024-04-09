using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


public partial class Autoload : Node
{
    public static Autoload instance;

    private List<Type> autoloadTypes = new List<Type>();
    public override void _Ready()
    {

        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (type.GetCustomAttributes(typeof(AutoloadAttribute), false).Length > 0)
            {
                autoloadTypes.Add(type);
            }
        }
        autoloadTypes.Sort((y,x)=>x.GetCustomAttribute<AutoloadAttribute>().priority.CompareTo(y.GetCustomAttribute<AutoloadAttribute>().priority));
        GD.Print($"Autoload: found {autoloadTypes.Count} Types");
        foreach (Type type in autoloadTypes)
        {
            GD.Print($"\tPriority: {type.GetCustomAttribute<AutoloadAttribute>().priority} Type: {type.FullName}");

            try
            {
                type.GetMethod("_Ready",BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Exception on {type.FullName}._Ready():\n{ex}");
            }
        }
    }

    public override void _EnterTree()
    {
        instance = this;
    }

    public override void _ExitTree()
    {

        foreach (Type type in autoloadTypes)
        {
            try
            {
                type.GetMethod("_ExitTree", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)?.Invoke(null,null);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Exception on {type.FullName}._ExitTree():\n{ex}");
            }
        }
    }

    public override void _Process(double delta)
    {
        foreach (Type type in autoloadTypes)
        {
            try
            {
                type.GetMethod("_Process", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)?.Invoke(null, new object[] { delta });
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Exception on {type.FullName}._Process():\n{ex}");
            }
        }
    }


    public override void _PhysicsProcess(double delta)
    {
        foreach (Type type in autoloadTypes)
        {
            try
            {
                type.GetMethod("_PhysicsProcess", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)?.Invoke(null, new object[] { delta });
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Exception on {type.FullName}._PhysicsProcess():\n{ex}");
            }
        }
    }
}

public class AutoloadAttribute : Attribute
{
    public int priority = 0;
    public AutoloadAttribute(int priority = 0)
    {
        this.priority = priority;
    }
}