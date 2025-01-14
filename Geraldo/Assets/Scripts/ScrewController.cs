﻿using UnityEngine;
using System.Collections;

public class ScrewController : MonoBehaviour
{
    [SerializeField]
    private float resetTime = 2.0f;

    [Header("ROTATION LIMITS")]
    [SerializeField]
    private float rotationFromMiddle = 2;
    private float _directedRotationUntilTightened => -rotationFromMiddle;

    private float _directedRotationsUntilPopOut => rotationFromMiddle;

    [SerializeField]
    [Range(0.1f, 0.9f)]
    private float initialScrewPosition = 0.5f;

    //Color change Vars
    [Header("COLORS")]
    [SerializeField]
    private string matPropertyToChange = "_Color";
    [SerializeField]
    private Color tightenedColor = Color.red;
    [SerializeField]
    private Color loosenedColor = Color.blue;
    [SerializeField]
    private Color defaultColor = Color.white;



    [Header("DO-NOT-MODIFY PROPERTIES")]
    [SerializeField]
    private GameObject screwPrefab;
    [SerializeField]
    private float playerRotationDir = -1;
    [SerializeField]
    private float counter = 0;
    public bool hasPlayer = false;
    public float rotationOnLatch = 0f;
    [SerializeField]
    GameObject nut;
    [SerializeField]
    private Transform lugNutTransform;

    //[Header("LIGHT BULBS")]
    //[SerializeField]
    //private LightBulbController[] lightBulbs;

    //Private Vars
    private PlayerMovementController pController;

    private Material screwMat;

    private Rigidbody screwRB;

    private Animator anim;

    private bool turningEnabled = true;

    private bool fullyTightened = false;

    private bool resetTimerStarted = false;

    private void Start()
    {
        InitializeCrap();
    }

    private float _angleLastFrame = 0;
    private float _angleEslaped = 0f;

    private void Update()
    {
        //rotationCounter will be pulled from player script
        if (turningEnabled && hasPlayer)
        {
            float angleDif = Mathf.Min(
                Mathf.Abs(pController._angle - _angleLastFrame),
                Mathf.Abs(Mathf.Abs(pController._angle - _angleLastFrame) - 360));

            _angleEslaped += angleDif * playerRotationDir;

            counter = _angleEslaped / 360f;
            _angleLastFrame = pController._angle;

            

            

           
            
            if (!fullyTightened)
            {
               // UpdateLightBulbIntensityScale();
                float screwAngle = counter * 360;
                lugNutTransform.eulerAngles = new Vector3(screwAngle, -90, 90);
                UpdateAnimation();

                if (counter <= _directedRotationUntilTightened)
                    SetTightended();
                else if (counter >= _directedRotationsUntilPopOut)
                    ScrewFallOut();
            }
            else
            {
                if (playerRotationDir == 1)
                {
                    _angleEslaped = _directedRotationUntilTightened + 0.3f;
                    SetBackToNorm();
                }
            }
        }
    }

    //private void UpdateLightBulbIntensityScale()
    //{
    //    if (lightBulbs == null || lightBulbs.Length == 0)
    //        return;

    //    float rotationPercent = Mathf.InverseLerp(
    //        _directedRotationUntilTightened,
    //        _directedRotationsUntilPopOut,
    //        -1 * counter);

    //    foreach (var bulb in lightBulbs)
    //        bulb.SetIntensityScale(rotationPercent, (int)playerRotationDir);
    //}

    private void OnValidate()
    {
        rotationFromMiddle = Mathf.Max(0.1f, rotationFromMiddle);
        counter = Mathf.Lerp(_directedRotationUntilTightened, _directedRotationsUntilPopOut, initialScrewPosition);
    }

    public void OnPlayerLatch()
    {
        hasPlayer = true;

        playerRotationDir = pController._rotationDirection;
        //_angleEslaped = 0f;
        _angleLastFrame = pController._angle;
        rotationOnLatch = pController._angle;
    }

    public void OnPlayerDetach()
    {

        hasPlayer = false;
    }

    private void UpdateAnimation()
    {
        float percent = Mathf.InverseLerp(_directedRotationUntilTightened, _directedRotationsUntilPopOut, -1 * counter);
        percent = Mathf.Min(percent, 0.97f); //1 reset clip to 0:00
        anim.SetFloat("AnimationTime", percent);
    }

    /// <summary>
    /// Undoes the fully tightened thing and allows the screw to move as normal again
    /// </summary>
    private void SetBackToNorm()
    {
        fullyTightened = false;
        screwMat.SetColor(matPropertyToChange, defaultColor);
    }

    /// <summary>
    /// Flips turning bool,
    /// stops player's momentum?
    /// </summary>
    private void SetTightended()
    {
        fullyTightened = true;
        screwMat.SetColor(matPropertyToChange, tightenedColor);
        //pController.ReleaseGrapple();
        //stop player's momentum?
    }


    /// <summary>
    ///turns on physics and dissable animator for screw object, 
    ///turns off this thing's grapple collision, 
    ///makes player ungrapple if grappled
    ////// <summary>
    private void ScrewFallOut()
    {
        turningEnabled = false;
        screwRB.useGravity = true;
        screwRB.isKinematic = false;
        anim.enabled = false;
        screwMat.SetColor(matPropertyToChange, loosenedColor);
        pController.ReleaseGrapple();

        if(!resetTimerStarted)
            StartCoroutine(ResetScrewTimer());
    }



    public void ResetScrewCompletely()
    {
        ScrewController newScrew = Instantiate(screwPrefab, transform.position, transform.rotation).GetComponent<ScrewController>();

        //setup Vars to match this screw's vars
        newScrew.rotationFromMiddle = rotationFromMiddle;
        newScrew.initialScrewPosition = initialScrewPosition;
        newScrew.resetTime = resetTime;
        newScrew.defaultColor = defaultColor;
        newScrew.loosenedColor = loosenedColor;
        newScrew.tightenedColor = tightenedColor;
        newScrew.matPropertyToChange = matPropertyToChange;
        newScrew.InitializeCrap();

        Destroy(this.gameObject);
    }


    private IEnumerator ResetScrewTimer()
    {
        resetTimerStarted = true;
        yield return new WaitForSeconds(resetTime);
        ResetScrewCompletely();
    }


    
    public void InitializeCrap()
    {
        pController = FindObjectOfType<PlayerMovementController>();

        //setup component connections
        anim = nut.GetComponent<Animator>();
        screwRB = nut.GetComponent<Rigidbody>();
        screwMat = nut.GetComponent<Renderer>().material;

        //reset screw
        nut.transform.position = new Vector3(0, 1, 0);
        nut.transform.eulerAngles = Vector3.zero;
        screwRB.useGravity = false;
        screwRB.isKinematic = true;
        anim.enabled = true;
        screwMat.SetColor(matPropertyToChange, defaultColor);

        anim.SetFloat("AnimationTime", initialScrewPosition);
        counter = Mathf.Lerp(_directedRotationUntilTightened, _directedRotationsUntilPopOut, initialScrewPosition);
    }
}
