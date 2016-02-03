using UnityEngine;
using System.Collections;

public class CameraLERP : MonoBehaviour
{
    Transform target;
    bool lockToTarget;
    bool lerpToTarget;
    bool orbitTarget;
    bool matchRotation;
    Vector3 oldLocalPos;   // made these accessible so we can LERP back to them.
    Quaternion oldLocalRot;
	public Vector3 oldWorldPos;   // made these accessible so we can LERP back to them.
    public Quaternion oldWorldRot;

    // Lerp variables
    float transitionTime;
    float timer;
    Vector3 oldPosition;
    Quaternion oldRotation;

    // Orbit variables
    protected float distance = 0.0f;

    public float xSpeed = 250.0f;
    public float ySpeed = 120.0f;

    public int yMinLimit = -20;
    public int yMaxLimit = 80;

    private float x = 0.0f;
    private float y = 0.0f;

    protected int orbitHorizontal = 0;
    protected int orbitVertical = 0;

    // Mouse Look variables
    public bool useMouseLook = false;
    public float sensitivityX = 15F;
    public float sensitivityY = 15F;

    public float minimumX = -360F;
    public float maximumX = 360F;

    public float minimumY = -60F;
    public float maximumY = 60F;

    protected float rotationY = 0F;

    protected int lookHorizontal = 0;
    protected int lookVertical = 0;

    // Screen edge variables
    public bool useScreenEdging = false;
    public bool displayEdging = false;
    public int horizontalSize = 10;
    public int verticalSize = 10;

    // Camera component
    Camera cam;

    // Use this for initialization
    void Start()
    {
        cam = gameObject.camera;
        if (cam == null)
            enabled = false;

        lockToTarget = false;
        lerpToTarget = false;
        orbitTarget = false;
        matchRotation = false;

        oldLocalPos = transform.localPosition;
        oldLocalRot = transform.localRotation;
        oldWorldPos = transform.position;
        oldWorldRot = transform.rotation;
    }

    void Update()
    {
        if (useScreenEdging)
        {
            GUIManager guiMgr = GUIManager.GetInstance();
            if (guiMgr == null || !guiMgr.IsModal())
            {
                if (Input.mousePosition.x < horizontalSize && Input.mousePosition.x > 0)
                    lookHorizontal = -1;
                else if (Input.mousePosition.x > Screen.width - horizontalSize && Input.mousePosition.x < Screen.width)
                    lookHorizontal = 1;

                if (Input.mousePosition.y < verticalSize && Input.mousePosition.y > 0)
                    lookVertical = -1;
                else if (Input.mousePosition.y > Screen.height - verticalSize && Input.mousePosition.y < Screen.height)
                    lookVertical = 1;
            }
        }
    }

    void LateUpdate()
    {
        if (useMouseLook && !matchRotation && !lerpToTarget)
        {
            GUIManager guiMgr = GUIManager.GetInstance();
            if (guiMgr == null || !guiMgr.IsModal())
            {
                if (Input.GetMouseButton(1))
                {
                    float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX * 0.02f;
                    rotationY += Input.GetAxis("Mouse Y") * sensitivityY * 0.02f;
                    rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                    transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
                }
                else if (lookHorizontal != 0 || lookVertical != 0)
                {
                    float rotationX = transform.localEulerAngles.y + lookHorizontal * sensitivityX * 0.01f;

                    rotationY += lookVertical * sensitivityY * 0.01f;
                    rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                    transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
                }
            }
        }
        lookHorizontal = lookVertical = 0;

        if (target == null)
            return;

        // LERP to taret
        if (lerpToTarget)
        {
            timer += Time.deltaTime;

            // Clamp to destination if timer great than set time
            if (timer >= transitionTime)
            {
                cam.transform.position = target.position + (target.rotation * new Vector3(0, 0, -distance));
                if (matchRotation)
                    cam.transform.rotation = target.rotation;
                //target = null;
                transitionTime = 0;
                timer = 0;
                lerpToTarget = false;
            }
            else
            {
                // Calculate distance and rotation change
                Vector3 distanceTo = (target.position + (target.rotation * new Vector3(0, 0, -distance))) - oldPosition;
                distanceTo = distanceTo * (timer / transitionTime);
                cam.transform.position = oldPosition + distanceTo;

                if (matchRotation)
                {
                    float angleTo = Quaternion.Angle(oldRotation, target.rotation);
                    angleTo *= Time.deltaTime / transitionTime;
                    cam.transform.rotation = Quaternion.RotateTowards(cam.transform.rotation, target.rotation, angleTo);
                }
            }
        }
        else
        {
            if (orbitTarget)
            {
                x += orbitHorizontal * xSpeed * 0.02f;
                y -= orbitVertical * ySpeed * 0.02f;

                orbitHorizontal = 0;
                orbitVertical = 0;

                if (y < -360)
                    y += 360;
                if (y > 360)
                    y -= 360;
                Mathf.Clamp(y, yMinLimit, yMaxLimit);

                Quaternion rotation = Quaternion.Euler(y, x + 180, 0);
                Vector3 temp = new Vector3(0, 0, -distance);
                Vector3 position = rotation * temp + target.position;

                cam.transform.rotation = rotation;
                cam.transform.position = position;
            }
            else if (lockToTarget)
            {
                cam.transform.position = target.position + (target.rotation * new Vector3(0, 0, -distance));
                if (matchRotation)
                    cam.transform.rotation = target.rotation;
            }
        }
    }

    void OnGUI()
    {
        if (useScreenEdging && displayEdging)
        {
            if (Input.mousePosition.x < horizontalSize && Input.mousePosition.x > 0)
                GUI.Box(new Rect(0, 0, horizontalSize, Screen.height), "");
            else if (Input.mousePosition.x > Screen.width - horizontalSize && Input.mousePosition.x < Screen.width)
                GUI.Box(new Rect(Screen.width - horizontalSize, 0, horizontalSize, Screen.height), "");

            if (Input.mousePosition.y < verticalSize && Input.mousePosition.y > 0)
                GUI.Box(new Rect(0, Screen.height - verticalSize, Screen.width, Screen.height), "");
            else if (Input.mousePosition.y > Screen.height - verticalSize && Input.mousePosition.y < Screen.height)
                GUI.Box(new Rect(0, 0, Screen.width, verticalSize), "");
        }
    }

    public void MoveTo(Transform transform, float overTime, bool lockTo, bool rotation)
    {
        distance = 0;
        timer = 0;
        target = transform;
        transitionTime = overTime;
        lockToTarget = lockTo;
        lerpToTarget = true;
        matchRotation = rotation;

        oldPosition = cam.transform.position;
        oldRotation = cam.transform.rotation;
		oldWorldPos = cam.transform.position; // public, will ease back to these
        oldWorldRot = cam.transform.rotation;
    }

    public void MoveTo(GameObject gameObject, float overTime, bool lockTo, bool rotation)
    {
        MoveTo(gameObject.transform, overTime, lockTo, rotation);
    }

    public void MoveTo(string objectName, float overTime, bool lockTo, bool rotation)
    {
        GameObject temp = GameObject.Find(objectName);
        if (temp != null)
            MoveTo(temp.transform, overTime, lockTo, rotation);
    }

    public void MoveTo(Transform transform, float overTime, bool lockTo, bool rotation, float offset)
    {
        MoveTo(transform, overTime, lockTo, rotation);
        distance = offset;
    }
    public void MoveTo(GameObject gameObject, float overTime, bool lockTo, bool rotation, float offset)
    {
        MoveTo(gameObject, overTime, lockTo, rotation);
        distance = offset;
    }
    public void MoveTo(string objectName, float overTime, bool lockTo, bool rotation, float offset)
    {
        MoveTo(objectName, overTime, lockTo, rotation);
        distance = offset;
    }

    public void Orbit(Transform transform, float overTime, float offset)
    {
        orbitTarget = true;
        MoveTo(transform, overTime, false, false, offset);
    }

    public void Orbit(GameObject gameObject, float overTime, float offset)
    {
        Orbit(gameObject.transform, overTime, offset);
    }

    public void Orbit(string objectName, float overTime, float offset)
    {
        GameObject temp = GameObject.Find(objectName);
        if (temp != null)
            Orbit(temp.transform, overTime, offset);
    }

    public void Reset()
    {
        lockToTarget = false;
        lerpToTarget = false;
        orbitTarget = false;
        transitionTime = 0;
        timer = 0;
        target = null;
        distance = 0;
    }

    public void Return()
    {
        Reset();
        transform.localPosition = oldLocalPos;
        transform.localRotation = oldLocalRot;
    }

    public void OrbitLeft()
    {
        if (orbitTarget)
            orbitHorizontal = 1;
    }
    public void OrbitRight()
    {
        if (orbitTarget)
            orbitHorizontal = -1;
    }
    public void OrbitUp()
    {
        if (orbitTarget)
            orbitVertical = -1;
    }
    public void OrbitDown()
    {
        if (orbitTarget)
            orbitVertical = 1;
    }

    public void LookLeft()
    {
        lookHorizontal = -1;
    }
    public void LookRight()
    {
        lookHorizontal = 1;
    }
    public void LookUp()
    {
        lookVertical = 1;
    }
    public void LookDown()
    {
        lookVertical = -1;
    }

    public void RotateTowards(Transform to)
    {
        Quaternion lookTo = new Quaternion();
        lookTo.SetLookRotation((to.transform.position - cam.transform.position).normalized);
        cam.transform.rotation = Quaternion.RotateTowards(cam.transform.rotation, lookTo, sensitivityX * Time.deltaTime);
        rotationY = -cam.transform.localEulerAngles.x;
    }
    public void RotateTowards(Vector3 to)
    {
        Quaternion lookTo = new Quaternion();
        lookTo.SetLookRotation((to - cam.transform.position).normalized);
        cam.transform.rotation = Quaternion.RotateTowards(cam.transform.rotation, lookTo, sensitivityX * Time.deltaTime);
        rotationY = -cam.transform.localEulerAngles.x;
    }

    public void LookAt(Vector3 to)
    {
        Quaternion lookAt = new Quaternion();
        lookAt.SetLookRotation((to - cam.transform.position).normalized);
        cam.transform.rotation = lookAt;
        rotationY = -cam.transform.localEulerAngles.x;
    }
}
