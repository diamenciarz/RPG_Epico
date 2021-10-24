using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashIconController : MonoBehaviour
{

    [SerializeField] float snapRange = 1.5f;

    bool isVisible = true;
    bool isDashAvailable = true;
    GameObject playerToFollow;
    GameObject currentClosestGameObject;
    SpriteRenderer mySpriteRenderer;
    PlayerMovement playerMovement;
    // Start is called before the first frame update
    private void OnEnable()
    {
        EventManager.StartListening("SetPlayerGameObject", SetGameObjectToFollow);
    }
    private void OnDisable()
    {
        EventManager.StopListening("SetPlayerGameObject", SetGameObjectToFollow);
    }
    private void Start()
    {
        mySpriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isDashAvailable)
        {
            UpdatePosition();
            UpdateRotation();
            UpdateVisibility();
        }
        else
        {
            SetVisibility(false);
        }
    }
    private void UpdateVisibility()
    {
        if (isVisible && playerMovement.CanDash())
        {
            SetVisibility(true);
        }
        else
        {
            SetVisibility(false);
        }
    }
    private void SetVisibility(bool isTrue)
    {
        mySpriteRenderer.enabled = isTrue;
    }
    private void UpdateRotation()
    {
        gameObject.transform.rotation = StaticDataHolder.GetRotationFromToIn2D(gameObject.transform.position, playerToFollow.transform.position);
    }
    private void UpdatePosition()
    {
        currentClosestGameObject = FindTheClosestDashableObject();
        StaticDataHolder.SetCurrentDashObject(currentClosestGameObject);

        if (currentClosestGameObject != null)
        {
            isVisible = true;
            GoToTheClosestDashableGameObject(currentClosestGameObject);
        }
        else
        {
            isVisible = false;
        }
    }
    private void GoToTheClosestDashableGameObject(GameObject goTo)
    {
        Vector3 objectPosition = goTo.transform.position;
        objectPosition.z = 0;
        transform.position = objectPosition;
    }
    private GameObject FindTheClosestDashableObject()
    {
        List<GameObject> dashableObjectList = StaticDataHolder.GetDashableObjectList();
        Vector3 playerPosition = playerToFollow.transform.position;
        GameObject returnObject = StaticDataHolder.FindTheClosestObjectInList(dashableObjectList, playerPosition);
        if (StaticDataHolder.GetDistanceBetweenObjectsIn2D(playerToFollow, returnObject) <= snapRange)
        {
            return returnObject;
        }
        else
        {
            return null;
        }
    }
    public void SetGameObjectToFollow(object inputObject)
    {
        try
        {
            playerToFollow = (GameObject)inputObject;
            playerMovement = playerToFollow.GetComponent<PlayerMovement>();
        }
        catch (System.Exception)
        {
            Debug.LogError("Object to follow was not an object");
            throw;
        }
    }
    public bool GetIsVisible()
    {
        return isVisible;
    }
    public void SetIsDashAvailable(bool isTrue)
    {
        isDashAvailable = isTrue;
    }
}
