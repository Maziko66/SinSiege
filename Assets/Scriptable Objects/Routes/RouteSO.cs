using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RouteSO", menuName = "Scriptable Objects/RouteSO")]
public class RouteSO : ScriptableObject
{
    [SerializeField] private List<GameObject> routePoints = new List<GameObject>();
    
    public List<GameObject> Routes => routePoints;

}
