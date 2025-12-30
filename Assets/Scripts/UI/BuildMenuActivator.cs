using System;
using UnityEngine;

public class BuildMenuActivator : MonoBehaviour
{
    private Player _player;
    private GameManager _gameManager;
    private BuildManager _buildManager;

    private void Awake()
    {
        _player = FindFirstObjectByType<Player>();
        _gameManager = FindFirstObjectByType<GameManager>();
        _buildManager = FindFirstObjectByType<BuildManager>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision.gameObject.tag);
        if(collision.gameObject.CompareTag("Tower Zone"))
        {
            Debug.Log("checker touched tower zone");
            // TowerZone towerZone = collision.GetComponent<TowerZone>();
            // lastTouchedTowerZone = towerZone.gameObject;
            // //Vector3 instantiatePosition = _cam.WorldToScreenPoint(collision.transform.position);
            // //Vector3 instantiatePosition = _cam.WorldToScreenPoint(transform.position);
            // if(towerZone.isEmpty)
            // {
            //     _buildManager.DrawUITowerBuilderCombat();
            //     Debug.Log("On Tower Zone Empty");
            // }
            // else
            // {
            //     _buildManager.DrawUITowerManagerCombat();
            //     Debug.Log("On Tower Zone Full");
            // }
            _player.BuildManagerState(collision, true);
            if (!_gameManager.onBuildMenu)
            {
                _buildManager.TowerZoneExpSliderActive(true);
            }
            
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Tower Zone"))
        {
            Debug.Log("checker left tower zone");
            // TowerZone towerZone = collision.GetComponent<TowerZone>();
            // if(towerZone.isEmpty)
            // {
            //     _buildManager.DestroyUITowerBuilderCombat();
            //     lastTouchedTowerZone = null;
            //     Debug.Log("Left Empty Tower Zone");
            // }
            // else
            // {
            //     _buildManager.DestroyUITowerManagerCombat();
            //     lastTouchedTowerZone = null;
            //     Debug.Log("Left Full Tower Zone");
            // }
            _player.BuildManagerState(collision, false);
            _buildManager.TowerZoneExpSliderActive(false);
        }
    }
}
