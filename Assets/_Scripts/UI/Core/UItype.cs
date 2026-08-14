using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
///
/// </summary>
public class UItype
{
    public string Name{get;private set;}
    public string Path{get;private set;}

    public UItype(string path)
    {
        Path = path;
        Name = path.Substring(path.LastIndexOf('/')+1);
    }
}
