using System.Linq.Expressions;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Canvas canvas;
    private Player player;

    #region GAMEOBJECTS
    [Header("Towers")]
    [SerializeField] private GameObject _parentTowers;
    [SerializeField] private Tower _towerTest; 
    #endregion
    #region UI
    [Header("UI")]
    [SerializeField] private UITowerBuilderCombat _UITowerBuilderCombat;
    [SerializeField] private GameObject _instantiatedUITowerBuilderCombat;
    [SerializeField] private UITowerManagerCombat _UITowerManagerCombat;
    [SerializeField] private GameObject _instantiatedUITowerManagerCombat;
    #endregion

    private void Awake()
    {
        canvas = FindFirstObjectByType<Canvas>();
        player = FindFirstObjectByType<Player>();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    public void DrawUITowerBuilderCombat(Vector3 pos)
    {
        if (_instantiatedUITowerBuilderCombat == null)
        {
            DestroyUITowerBuilderCombat();
        }
        _instantiatedUITowerBuilderCombat = Instantiate(_UITowerBuilderCombat.gameObject, canvas.gameObject.transform);
        _instantiatedUITowerBuilderCombat.transform.position = pos;
        
    }

    public void DestroyUITowerBuilderCombat()
    {
        Destroy(_instantiatedUITowerBuilderCombat.gameObject);
    }

    public void DrawUITowerManagerCombat(Vector3 pos)
    {
        if(_instantiatedUITowerManagerCombat == null)
        {
            DestroyUITowerManagerCombat();
        }
        _instantiatedUITowerManagerCombat = Instantiate(_UITowerManagerCombat.gameObject, canvas.gameObject.transform);
        _instantiatedUITowerManagerCombat.transform.position = pos;

    }

    public void DestroyUITowerManagerCombat()
    {
        Destroy(_instantiatedUITowerManagerCombat.gameObject);
    }

    public void TowerCreateTest()
    {
        if(player.lastTouchedTowerZone == null)
        {
            Debug.Log("lastTouchedTowerZone == null");
            return;
        }

        TowerZone zone = player.lastTouchedTowerZone.GetComponent<TowerZone>();

        if (!zone.isEmpty)
        {
            Debug.Log("zone is not empty");
            return;
        }

        GameObject newTower = Instantiate(_towerTest.gameObject, _parentTowers.transform);
        newTower.transform.position = player.lastTouchedTowerZone.transform.position;

        zone.occupyingTower = newTower.GetComponent<Tower>();
        zone.isEmpty = false;

        DestroyUITowerBuilderCombat();
    }

    public void TowerDestroy()
    {
        if(player.lastTouchedTowerZone == null)
        {
            Debug.Log("lastTouchedTowerZone == null");
            return;
        }

        TowerZone zone = player.lastTouchedTowerZone.GetComponent<TowerZone>();

        if(zone.isEmpty)
        {
            Debug.Log("zone is empty");
            return;
        }
        Destroy(zone.occupyingTower.gameObject);
        zone.isEmpty = true;

        DestroyUITowerManagerCombat();
    }
}
