using UnityEngine;

[ExecuteAlways]
public class PortalOld : MonoBehaviour
{
    public PortalOld partner; // The linked portal
    public Camera portalCamera; // The camera rendering the portal view
    public Renderer portalScreen; // The mesh displaying the portal texture
    private RenderTexture portalTexture; // Texture for rendering

    private void Awake()
    {
        if (partner == null || portalCamera == null || portalScreen == null)
        {
            Debug.LogError("Portal is missing references.");
            return;
        }

        // Create and assign render texture
        portalTexture = new RenderTexture(Screen.width, Screen.height, 24);
        portalCamera.targetTexture = portalTexture;
        portalScreen.material.mainTexture = portalTexture;
    }

    private void LateUpdate()
    {
        if (partner == null || portalCamera == null) return;

        AlignPortalCamera();
    }

    private void AlignPortalCamera()
    {
        Transform mainCam = Camera.main.transform;
        Transform camTrans = portalCamera.transform;
        Transform partnerTrans = partner.transform;

        Vector3 cameraEuler = Vector3.zero;

        // Position: Convert main camera's position relative to this portal, then apply to partner portal
        Vector3 localPosition = transform.InverseTransformPoint(mainCam.position);
        camTrans.localPosition = new Vector3(-localPosition.x, localPosition.y, -localPosition.z);
        camTrans.position = partnerTrans.TransformPoint(camTrans.localPosition);

        // Find the x-rotation
        Transform prevParent = mainCam.parent;
        mainCam.SetParent(transform);
        cameraEuler.x = mainCam.localEulerAngles.x;
        mainCam.SetParent(prevParent);

        // Find the y-rotation using SignedAngle
        Vector3 oldPlayerRot = mainCam.localEulerAngles;
        mainCam.localRotation = Quaternion.Euler(0, oldPlayerRot.y, oldPlayerRot.z);
        cameraEuler.y = SignedAngle(-partnerTrans.forward, mainCam.forward, Vector3.up);
        mainCam.localRotation = Quaternion.Euler(oldPlayerRot);

        // Apply rotation
        camTrans.localRotation = Quaternion.Euler(cameraEuler);
    }

    private float SignedAngle(Vector3 a, Vector3 b, Vector3 n)
    {
        float angle = Vector3.Angle(a, b);
        float sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(a, b)));
        float signed_angle = angle * sign;

        while (signed_angle < 0) signed_angle += 360;

        return signed_angle;
    }
}
