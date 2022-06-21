using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveCtrl : MonoBehaviour
{

    #region ### Parameters ###

    [System.Serializable]
    public struct Tire
    {
        [Header("Transform")]
        public Transform tfm;

        [Header("Speed")]
        public float tireAccel;
        public Vector3 tireLocalVel;

        [Header("Torque")]
        public float nowTorque;
        /// <summary>
        /// resistTorque*ギア比
        /// </summary>
        public float nowResistTorque;
        public float nowBrakeTorque;

        [Header("Grip")]
        public float nowGripForward;
        public float nowGripSideways;
        /// <summary>
        /// 駆動力（ローカル）
        /// </summary>
        public Vector3 gripForce;
        public Vector3 gripNormalizedForce;

        [Header("Slip")]
        /// <summary>
        /// タイヤのスリップ率
        /// （+：車速＜タイヤ速、-：車速＞タイヤ速、０：滑ってない）
        /// </summary>
        public float slipRate;
        /// <summary>
        /// タイヤのスリップ率の最大値
        /// </summary>
        public float maxSlipRate;

        /// <summary>
        /// すべり角
        /// </summary>
        public float slipRot;
        /// <summary>
        /// タイヤ速 - 車速
        /// </summary>
        public Vector3 diffSpeed;

        public Vector3 TireSpeed { get { return tfm.TransformVector(tireLocalVel); } }
        public Vector3 TireLocalSpeed { get { return tireLocalVel; } }
        public float TireLocalSpeedLength { get { return tireLocalVel.x * tireLocalVel.x + tireLocalVel.z * tireLocalVel.z; } }

        [Header("Other")]
        public float mass;
    }
    public enum MissionType
    {
        CVT
    }

    [Header("Component")]
    [SerializeField] Rigidbody rigid;
    [SerializeField] PlayerCondition condition;
    public float Weight { get { return rigid.mass; } }

    [Header("Effect")]
    [SerializeField] ParticleSystem particleTireSmoke;

    [Header("Speed")]
    Vector3 preVel;
    Vector3 nowVel;

    /// <summary>
    /// 速度【km/h】（プレイヤーの向きに依存しないワールドでの速度）
    /// </summary>
    public Vector3 BodySpeed { get { return rigid.velocity * 3.6f; } }
    /// <summary>
    /// 速度【km/h】（プレイヤーの向きからみたローカル速度）
    /// </summary>
    public Vector3 BodyLocalSpeed { get { return transform.InverseTransformVector(BodySpeed); } }
    public float BodyLocalSpeedLength { get { return BodyLocalSpeed.x * BodyLocalSpeed.x + BodyLocalSpeed.z * BodyLocalSpeed.z; } }
    public Vector3 AngularSpeed { get { return rigid.angularVelocity; } }

    [Header("Rotate Speed")]
    float rotateVal = 0;
    float maxRotateVal = 6f;
    float rotateRegist = 1f;

    [Header("Body Status")]
    /// <summary>
    /// 足の長さ（タイヤの直径と同様に扱う）
    /// </summary>
    [SerializeField] float insideLeg = 0.7f;
    [SerializeField] [Range(0, 1)] float InertialForceRate = 0.01f;
    /// <summary>
    /// 足の長さ（タイヤの直径と同様に扱う）
    /// </summary>
    public float LengthLeg { get { return insideLeg; } }

    #region ### Parameter / Tire ###

    [Header("Tire")]
    public Tire[] tires = new Tire[4];
    [SerializeField] Transform tireWholeTfm;
    /// <summary>
    /// 前後の駆動配分（0：前だけ、1：後ろだけ）
    /// </summary>
    [Range(0, 1)] public float drivedistributionForward = 0.5f;
    /// <summary>
    /// 左右の駆動配分（0：左だけ、1：右だけ）
    /// </summary>
    [Range(0, 1)] public float drivedistributionSideways = 0.5f;
    /// <summary>
    /// タイヤの座標軸から見たタイヤ全体の速度
    /// </summary>
    public Vector3 TireTotalLocalSpeed
    {
        get
        {
            return tires[0].TireLocalSpeed * (1 - drivedistributionForward) * drivedistributionSideways
                + tires[1].TireLocalSpeed * (1 - drivedistributionForward) * (1 - drivedistributionSideways)
                + tires[2].TireLocalSpeed * drivedistributionForward * drivedistributionSideways
                + tires[3].TireLocalSpeed * drivedistributionForward * (1 - drivedistributionSideways);
        }
    }
    /// <summary>
    /// タイヤ周長
    /// </summary>
    public float TireLength { get { return Mathf.PI * insideLeg; } }

    [Header("Grip")]
    [SerializeField] float[] gripForward = new float[11];
    [SerializeField] float[] gripSideways = new float[11];
    float gripRate;
    int gripRateNum;
    float inertiaRot;
    float accelDiffForward;
    float accelDiffSideways;
    public float[] GripForward { get { return gripForward; } set { gripForward = value; } }
    public float[] GripSideways { get { return gripSideways; } set { gripSideways = value; } }
    public float GripRate { get { return gripRate; } }

    /// <summary>
    /// 自分自身の摩擦係数(1=普通、0～1=滑る)
    /// </summary>
    [SerializeField] float tireCof = 1;
    /// <summary>
    /// 接地している地面の摩擦係数(1=普通、0～1=滑る)
    /// </summary>
    [SerializeField] float groundCof = 1;
    [SerializeField] float gripPowerRateForward = 1;
    [SerializeField] float gripPowerRateSideways = 1;

    public float Cof { get { return tireCof * groundCof; } }
    public float TireCof { get { return tireCof; } set { tireCof = value; } }
    public float GroundCof { get { return groundCof; } set { groundCof = value; } }
    public float GripPowerRateForward { get { return gripPowerRateForward; } set { gripPowerRateForward = value; } }
    public float GripPowerRateSideways { get { return gripPowerRateSideways; } set { gripPowerRateSideways = value; } }

    #endregion ### Parameter / Tire ###

    #region ### Parameter / Input ###

    [Header("Input")]
    public int playerNum;
    [SerializeField] [Range(-1, 1)] float steering;
    [SerializeField] [Range(-1, 1)] float upDown;
    [SerializeField] float steerMoveSpeed = 6;
    [SerializeField] [Range(0, 1)] float accel;
    [SerializeField] [Range(0, 1)] float brake;
    [SerializeField] [Range(0, 1)] float clutch;
    [SerializeField] Vector2 move;
    [SerializeField] float activeCalcRotValue = 0.1f;
    /// <summary>
    /// 入力された角度（＋：時計回り、－：反時計回り）
    /// </summary>
    [SerializeField] float inputMoveRot;
    public float SteerInput { get { return steering; } }
    public float AccelInput { get { return accel; } }
    public float BrakeInput { get { return brake; } }
    public float ClutchInput { get { return clutch; } }

    #endregion ### Parameter / Input ###

    #region ### Parameter / Rpm ###
    [Header("Rpm")]
    [SerializeField] float maxRpm = 10000;
    [SerializeField] float rpm;
    [SerializeField] [Range(0, 1)] float rpmRate;
    [SerializeField] float rpmUpPower = 2000;
    [SerializeField] float rpmDownPower = 1000;
    
    public float Rpm { get { return rpm; } }
    public float RpmRate { get { return rpmRate; } }
    public float MaxRpm { get { return maxRpm; } }

    public enum RevLimitType
    {
        None,
        Interval,
        AccelOff
    }
    [Header("Rpm/Limiter")]
    [SerializeField] RevLimitType revLimitType = RevLimitType.AccelOff;
    [SerializeField] float redZoneRpm = 9000;
    bool isRevLimited = false;

    [Header("Limiter/Interval")]
    [SerializeField] float intervalRevLimit = 0.2f;
    float timerRevLimit;
    [Header("Limiter/AccelOff")]
    [SerializeField] float accelOffStartRpm = 8000;

    [Header("Rpm/SpeedLimiter")]
    [SerializeField] float limitMaxSpeed = 100;
    
    [Header("Torque")]
    [SerializeField] float[] torque = new float[11];
    public float[] Torque { get { return torque; } set { torque = value; } }

    float torqueRate;
    int torqueRateNum;
    [SerializeField] float resistTorque = 10;
    [SerializeField] float brakeTorque = 10;

    #endregion ### Parameter / Rpm ###

    #region ### Parameter / Gear ###
    [Header("Gear")]
    [SerializeField] MissionType missionType = MissionType.CVT;
    [SerializeField] float[] gearRatio = new float[1];
    [SerializeField] float finalGearRatio = 4;
    [SerializeField] int nowGear = 0;
    public float TotalGearRatio { get { return gearRatio[nowGear] * finalGearRatio; } }
    public float FinalGearRatio { get { return finalGearRatio; } set { finalGearRatio = value; } }
    public float[] GearRatio { get { return gearRatio; } }

    [Header("Gear / CVT")]
    [SerializeField] float targetCvtRpm = 5000;
    [SerializeField] float minCvtRpm = 4000;
    [SerializeField] float maxCvtRpm = 6000;
    [SerializeField] float startGearRatio = 3.152f;
    [SerializeField] float endGearRatio = 0.577f;
    [SerializeField] float changeGearRatioSpeed = 0.2f;
    float bodyRpm;
    float cvtRpmRate;
    public float CVTStartGearRatio { get { return startGearRatio; } set { startGearRatio = value; } }
    public float CVTEndGearRatio { get { return endGearRatio; } set { endGearRatio = value; } }
    public float MaxCvtRpm { get { return maxCvtRpm; } set { maxCvtRpm = value; } }
    public float MinCvtRpm { get { return minCvtRpm; } set { minCvtRpm = value; } }

    #endregion ### Parameter / Gear ###

    #region ### Parameter / Assist ###
    
    [Header("VGS")]
    [SerializeField] float minVgsTargetSpeed = 5;
    [Tooltip("低速時の向きの変えやすさ\n高いと低速時の向き変えを素早く行える")]
    [SerializeField] [Range(0.1f, 10f)] float fastSteerMoveSpeed = 6;
    [SerializeField] float maxVgsTargetSpeed = 100;
    [Tooltip("高速時の向きの変えやすさ\n低いと高速時の向き変えがゆっくりになり、ふらつきにくい")]
    [SerializeField] [Range(0.1f, 10f)] float slowSteerMoveSpeed = 1;
    float vgsRate;

    [Header("DownForce")]
    [Range(0, 2000)] public float downforce = 500;

    #endregion ### Parameter / Assist ###

    #region ### Parameter / GroundCheck ###

    [Header("Ray")]
    [SerializeField] bool isGround;
    Ray rayGround;
    RaycastHit hitGround;
    [SerializeField] LayerMask rayLayerMask;
    [SerializeField] float hitGroundDistance = 1;
    /// <summary>
    /// 地面に飛ばすレイの最大距離
    /// </summary>
    [SerializeField] float rayDistance = 2;
    /// <summary>
    /// レイに当たった地面までの距離
    /// </summary>
    [SerializeField] float distanceFromGround;

    public bool IsGround { get { return isGround; } }

    [Header("Damping")]
    /// <summary>
    /// 自然長
    /// </summary>
    [SerializeField] float equilibriumLength = 1;
    /// <summary>
    /// ばねの伸びの最大値
    /// </summary>
    [SerializeField] float maxStretchLength = 2;
    /// <summary>
    /// ばねの縮みの最大値
    /// </summary>
    [SerializeField] float maxShrinkLength = 0.5f;
    /// <summary>
    /// 自然長からの距離（正：伸びている、負：縮んでいる）
    /// </summary>
    [SerializeField] float x;
    /// <summary>
    /// ばね定数
    /// </summary>
    [SerializeField] float k = 1000f;
    /// <summary>
    /// 減衰力
    /// </summary>
    [SerializeField] float b = -100f;
    [SerializeField] float dampingForce;

    #endregion ### Parameter / GroundCheck ###

    [Header("Other")]
    int i;

    #endregion ### Parameters ###

    #region ### Methods ###



    void Update()
    {
        ChangeGear();

        UpdateRevLimiter();

        UpdateRpm();

        AdjustSteerSpeed();

    }
    
    void FixedUpdate()
    {
        nowVel = rigid.velocity;

        RotateBody();
        AddInertialForce();
        GetGroundData();
        Damping();

        MoveInAir();

        for (i = 0; i < tires.Length; i++)
        {
            UpdateTire(ref tires[i]);
        }


        for (i = 0; i < tires.Length; i++)
        {
            AddTireForce(ref tires[i]);

        }

        EmitSmoke();
        
        AddDownForce();

        
    }
    
    #region ### Methods / Input ###
    
    public void UpdateInput(float steering, float upDown, float accel, float brake, float clutch)
    {
        this.steering = condition.confusion.isActive ? -steering : steering;
        this.upDown = condition.confusion.isActive ? -upDown : upDown;
        this.accel = accel;
        this.brake = brake;
        this.clutch = clutch;

        move.x = steering;
        move.y = upDown;

        if (move.magnitude >= activeCalcRotValue)
        {
            inputMoveRot = Mathf.Atan2(move.x, move.y) * Mathf.Rad2Deg;
        }
        else
        {
            inputMoveRot = 0;
        }
    }

    #endregion ### Methods / Input ###

    #region ### Methods / Power ###
    
    void UpdateRpm()
    {
        rpm = rpm * (1 - clutch) + TireTotalLocalSpeed.z * 1000 / 60f / TireLength * TotalGearRatio * clutch;

        // クラッチを切っている時
        rpm += (rpmUpPower * accel - rpmDownPower * (1 - accel)) * (1 - clutch) * GameStatusManager.NowTimeScale;

        if (rpm > maxRpm)
        {
            rpm = maxRpm;
        }
        else if (rpm < 0)
        {
            rpm = 0;
        }

        rpmRate = rpm / maxRpm;

        CalcTorque(ref tires[0], (1 - drivedistributionForward) * drivedistributionSideways);
        CalcTorque(ref tires[1], (1 - drivedistributionForward) * (1 - drivedistributionSideways));
        CalcTorque(ref tires[2], drivedistributionForward * drivedistributionSideways);
        CalcTorque(ref tires[3], drivedistributionForward * (1 - drivedistributionSideways));
    }

    void UpdateRevLimiter()
    {
        switch (revLimitType)
        {
            case RevLimitType.Interval:
                if (rpm >= redZoneRpm)
                {
                    isRevLimited = true;
                    timerRevLimit = 0;
                }

                if (isRevLimited)
                {
                    timerRevLimit += Time.deltaTime;

                    if (timerRevLimit >= intervalRevLimit)
                    {
                        isRevLimited = false;
                    }
                    else
                    {
                        accel = 0;
                    }
                }
                break;
            case RevLimitType.AccelOff:

                accel = Mathf.Min(accel, 1 - (rpm - accelOffStartRpm) / (redZoneRpm - accelOffStartRpm));

                break;
            default:
                break;
        }


    }

    void CalcTorque(ref Tire t, float driveDistribution)
    {
        torqueRateNum = Mathf.FloorToInt(rpmRate * (torque.Length - 1));
        torqueRate = rpmRate * (torque.Length - 1) - torqueRateNum;

        if (torqueRateNum >= (torque.Length - 1))
        {
            t.nowTorque = 0;
        }
        else
        {
            t.nowTorque = torque[torqueRateNum] * (1 - torqueRate) + torque[torqueRateNum + 1] * torqueRate;

            // 駆動配分の量に調整
            t.nowTorque *= driveDistribution;
        }
        // 抵抗トルクの計算
        t.nowResistTorque = resistTorque * TotalGearRatio;

        if (Mathf.Abs(t.tireLocalVel.z) < 5f)
        {

        }
        t.nowResistTorque = Mathf.Min(t.nowResistTorque, Mathf.Abs(t.tireLocalVel.z) * 10f);

        // 逆走しているときは逆に抵抗トルクがかかるようにする
        if (t.tireLocalVel.z < 0)
        {
            t.nowResistTorque *= -1;
        }

        t.nowTorque = t.nowTorque * accel - t.nowResistTorque * (1 - accel);

        // ブレーキのトルクの計算
        t.nowBrakeTorque = brake * brakeTorque;


    }

    void ChangeGear()
    {
        switch (missionType)
        {
            case MissionType.CVT:
                bodyRpm = Mathf.Sqrt(BodyLocalSpeedLength) * 1000 / 60f / TireLength * TotalGearRatio;

                // アクセル開度によってCVTの目標数を変える
                targetCvtRpm = minCvtRpm * (1 - accel) + maxCvtRpm * accel;

                if (bodyRpm < targetCvtRpm)
                {
                    cvtRpmRate = 1 - Mathf.Clamp01((bodyRpm - targetCvtRpm * 0.6f) / (targetCvtRpm * 0.4f));
                    gearRatio[0] += cvtRpmRate * changeGearRatioSpeed * GameStatusManager.NowTimeScale;
                    if (gearRatio[0] > startGearRatio)
                    {
                        gearRatio[0] = startGearRatio;
                    }
                }
                else
                {
                    cvtRpmRate = Mathf.Clamp01((bodyRpm - targetCvtRpm) / (maxRpm - targetCvtRpm));
                    gearRatio[0] -= cvtRpmRate * changeGearRatioSpeed * GameStatusManager.NowTimeScale;
                    if (gearRatio[0] < endGearRatio)
                    {
                        gearRatio[0] = endGearRatio;
                    }
                }

                break;
            default:
                break;
        }
    }

    #endregion ### Methods / Power ###

    #region ### Methods / Tire ###

    /// <summary>
    /// タイヤの速度を0にする
    /// </summary>
    public void StopTireSpeed()
    {
        for (i = 0; i < tires.Length; i++)
        {
            tires[i].tireLocalVel = Vector3.zero;
        }
    }

    void UpdateTire(ref Tire tire)
    {
        
        CalcTireSpeed(ref tire);

        CalcSlip(ref tire);
        CalcGrip(ref tire);


    }

    void CalcTireSpeed(ref Tire t)
    {
        t.tireAccel = (t.nowTorque * TotalGearRatio / insideLeg) / rigid.mass;

        t.tireLocalVel.z += t.tireAccel * Time.fixedDeltaTime;

        // ブレーキの処理
        t.tireAccel = (t.nowBrakeTorque * TotalGearRatio / insideLeg) / rigid.mass;
        if (t.tireLocalVel.z > 0)
        {
            t.tireLocalVel.z -= t.tireAccel * Time.fixedDeltaTime;
            if (t.tireLocalVel.z < 0)
            {
                t.tireLocalVel.z = 0;
            }
        }
        else
        {
            t.tireLocalVel.z += t.tireAccel * Time.fixedDeltaTime;
            if (t.tireLocalVel.z > 0)
            {
                t.tireLocalVel.z = 0;
            }
        }
    }

    void CalcSlip(ref Tire t)
    {
        if (Mathf.Abs(t.TireLocalSpeedLength) + Mathf.Abs(BodyLocalSpeedLength) == 0)
        {
            t.slipRate = 0;
        }
        else
        {
            // 平方根ではなく平方で計算
            // 分母は1km/h以上で計算する(止まりかけでスリップ率が激しく変動しないようにするため)
            t.slipRate = (t.TireLocalSpeedLength - BodyLocalSpeedLength) / Mathf.Max(1f, BodyLocalSpeedLength);
        }
        t.slipRate = t.slipRate / t.maxSlipRate;
        
        if (t.slipRate > 1)
        {
            t.slipRate = 1;
        }
        else if (t.slipRate < -1)
        {
            t.slipRate = -1;
        }
        
        if (!isGround)
        {
            t.slipRate = 0;
        }

        t.diffSpeed = t.TireLocalSpeed - BodyLocalSpeed;
        t.slipRot = Mathf.Atan2(t.diffSpeed.x, t.diffSpeed.z);
    }

    void CalcGrip(ref Tire t)
    {
        gripRateNum = Mathf.FloorToInt(Mathf.Abs(t.slipRate) * (gripForward.Length - 1));
        gripRate = Mathf.Abs(t.slipRate) * (gripForward.Length - 1) - gripRateNum;

        if (gripRateNum >= gripForward.Length - 1)
        {
            t.nowGripForward = gripForward[gripForward.Length - 1];
            t.nowGripSideways = gripSideways[gripSideways.Length - 1];
        }
        else
        {
            t.nowGripForward = gripForward[gripRateNum] * (1 - gripRate) + gripForward[gripRateNum + 1] * gripRate;
            t.nowGripSideways = gripSideways[gripRateNum] * (1 - gripRate) + gripSideways[gripRateNum + 1] * gripRate;

        }

        accelDiffForward = Mathf.Abs(t.diffSpeed.z) / Time.fixedDeltaTime;
        accelDiffSideways = Mathf.Abs(t.diffSpeed.x) / Time.fixedDeltaTime;

        // 駆動力の計算
        t.gripForce.z = accelDiffForward * t.nowGripForward * Cof * Mathf.Cos(t.slipRot) * gripPowerRateForward;
        t.gripForce.x = accelDiffSideways * t.nowGripSideways * Cof * Mathf.Sin(t.slipRot) * gripPowerRateSideways;

        // 速度差による力以上は出ないようにする
        if (t.diffSpeed.z > 0)
        {
            if (t.gripForce.z > t.diffSpeed.z / Time.fixedDeltaTime * t.mass)
            {
                t.gripForce.z = t.diffSpeed.z / Time.fixedDeltaTime * t.mass;
            }
        }
        else
        {
            if (t.gripForce.z < t.diffSpeed.z / Time.fixedDeltaTime * t.mass)
            {
                t.gripForce.z = t.diffSpeed.z / Time.fixedDeltaTime * t.mass;
            }
        }
    }

    #endregion ### Methods / Tire ###

    #region ### Methods / Move ###

    void AddTireForce(ref Tire t)
    {
        if (!isGround)
        {
            return;
        }

        t.gripNormalizedForce = Vector3.ProjectOnPlane(t.tfm.TransformVector(t.gripForce), hitGround.normal);
        rigid.AddForceAtPosition(t.gripNormalizedForce, t.tfm.position);



        // タイヤにも力をかける
        t.tireAccel = t.gripForce.z / t.mass;

        // 速度差による力以上は出ないようにする
        if (t.diffSpeed.z > 0)
        {
            if (t.tireAccel > t.diffSpeed.z)
            {
                t.tireAccel = t.diffSpeed.z;
            }
        }
        else
        {
            if (t.tireAccel < t.diffSpeed.z)
            {
                t.tireAccel = t.diffSpeed.z;
            }
        }

        t.tireLocalVel.z -= t.tireAccel * Time.fixedDeltaTime;
        

    }

    /// <summary>
    /// 慣性力を与える
    /// </summary>
    void AddInertialForce()
    {
        rigid.AddForce((nowVel - preVel) / Time.fixedDeltaTime * rigid.mass * InertialForceRate);
        preVel = nowVel;
    }

    void AddDownForce()
    {
        rigid.AddForce(0, -downforce, 0);
    }

    float preRotate = 0;
    
    /// <summary>
    /// キャラクターの向きを変える
    /// </summary>
    void RotateBody()
    {
        // 着地していない場合は旋回できない
        if (!isGround)
        {
            return;
        }
        
        rotateVal += steerMoveSpeed * steering;

        rotateVal = Mathf.Clamp(rotateVal, -maxRotateVal, maxRotateVal);

        transform.Rotate(0, rotateVal * GameStatusManager.NowTimeScale, 0);

        // 回転を抑制する力
        if (rotateVal > 0)
        {
            rotateVal -= rotateRegist * GameStatusManager.NowTimeScale;
            if(rotateVal < 0)
            {
                rotateVal = 0;
            }
        }
        else
        {
            rotateVal += rotateRegist * GameStatusManager.NowTimeScale;
            if (rotateVal > 0)
            {
                rotateVal = 0;
            }
        }
    }

    /// <summary>
    /// キャラクターが空中にいる時の挙動を制御
    /// </summary>
    void MoveInAir()
    {
        if (isGround)
        {
            transform.Rotate(-transform.localEulerAngles.x, 0, 0);
            return;
        }

        transform.Rotate(60f * move.y * Time.fixedDeltaTime, 0, 0);

        if (transform.localEulerAngles.x > 30)
        {
            transform.Rotate(30 - transform.localEulerAngles.x, 0, 0);
        }
        else if (transform.localEulerAngles.x < -30)
        {
            transform.Rotate(-30 + transform.localEulerAngles.x, 0, 0);
        }
    }

    #endregion ### Methods / Move ###

    #region ### Method / CheckGround ###

    /// <summary>
    /// 着地している地面の情報を取得する
    /// </summary>
    void GetGroundData()
    {
        
        CheckLanding();
    }

    /// <summary>
    /// 着地しているかどうかを計算する
    /// </summary>
    void CheckLanding()
    {
        rayGround.direction = transform.TransformDirection(Vector3.down);
        rayGround.origin = transform.position;

        // レイを飛ばして地面に届くかどうかを調べる
        Debug.DrawLine(rayGround.origin, rayGround.GetPoint(rayDistance), Color.green);
        isGround = Physics.Raycast(rayGround, out hitGround, rayDistance, rayLayerMask);
        if (isGround)
        {
            //Debug.Log("着地");
            Debug.DrawLine(rayGround.origin, rayGround.GetPoint(hitGround.distance), Color.red);
            distanceFromGround = hitGround.distance;
            
            tireWholeTfm.position = hitGround.point;

            CheckGroundTag();
        }
        else
        {
            tireWholeTfm.position = transform.position - transform.up * rayDistance;
        }
    }

    /// <summary>
    /// 着地している地面の種類を調べる
    /// </summary>
    void CheckGroundTag()
    {
        switch (hitGround.collider.tag)
        {
            case "Ice":
                // 氷の上
                groundCof = 0.2f;
                break;
            default:
                // 普通の地面
                groundCof = 1;
                break;
        }
    }

    /// <summary>
    /// サスペンションのはね返り具合を計算する
    /// </summary>
    void Damping()
    {
        x = distanceFromGround - equilibriumLength;

        if (isGround)
        {
            dampingForce = -k * x + b * rigid.velocity.y;
        }
        else
        {
            dampingForce = 0;
        }

        rigid.AddRelativeForce(0, dampingForce, 0);
    }

    #endregion ### Method / CheckGround ###

    #region ### Methods / Effect ###
    
    void EmitSmoke()
    {
        if (!isGround)
        {
            particleTireSmoke.emissionRate = 0;
            return;
        }

        particleTireSmoke.emissionRate = 80 * Mathf.Abs(Mathf.Max(tires[0].slipRate, tires[1].slipRate, tires[2].slipRate, tires[3].slipRate)) - 0.8f / 0.2f;
        particleTireSmoke.startSpeed = (BodyLocalSpeed.x + BodyLocalSpeed.z) / 100f;
    }

    void ShakeCamera()
    {
        GameStatusManager.cameraCtrls[playerNum].ShakeCameraInSpin(Mathf.Abs(Mathf.Max(tires[0].slipRate, tires[1].slipRate, tires[2].slipRate, tires[3].slipRate)));
    }

    #endregion ### Methods / Effect ###

    #region ### Method / Assist ###

    /// <summary>
    /// 速度を制限する
    /// </summary>
    void LimitSpeed()
    {
        
    }

    /// <summary>
    /// 旋回速度を調整する
    /// </summary>
    void AdjustSteerSpeed()
    {
        
        // 遅いときほど早くステアリングが切れるようにする
        vgsRate = Mathf.Clamp01((Mathf.Abs(BodyLocalSpeed.magnitude) - minVgsTargetSpeed) / (maxVgsTargetSpeed - minVgsTargetSpeed));
        
        steerMoveSpeed = fastSteerMoveSpeed * (1 - vgsRate) + slowSteerMoveSpeed * vgsRate;
        
    }

    #endregion ### Method / Assist ###

    #region ### RigidBody ###

    /// <summary>
    /// プレイヤーの体の速度を強制的に０にし、プレイヤーの動きを止める（足の速度は止めない）
    /// </summary>
    public void StopRbSpeed()
    {
        rigid.velocity = Vector3.zero;
    }

    /// <summary>
    /// キャラクターの体と足の速度を強制的に０にし、動きを止める
    /// </summary>
    public void StopRbAndTire()
    {
        StopTireSpeed();
        StopRbSpeed();
    }

    #endregion ### RigidBody ###

    #region ### Getter ###

    /// <summary>
    /// 最高速度を返す
    /// </summary>
    /// <returns></returns>
    public float GetMaxSpeed()
    {
        
        return (Mathf.PI * LengthLeg * MaxRpm * 60) / (1000 * CVTEndGearRatio * FinalGearRatio);
    }

    /// <summary>
    /// 最高加速度を返す
    /// </summary>
    /// <returns></returns>
    public float GetMaxAccel()
    {
        List<float> torqueList = GetTorqueList();

        float maxAccel = (torqueList.Max() * CVTStartGearRatio * FinalGearRatio / (LengthLeg / 2)) / Weight;

        return maxAccel;
    }

    /// <summary>
    /// トルクカーブのリストを返す
    /// </summary>
    /// <returns></returns>
    public List<float> GetTorqueList()
    {
        List<float> torqueList = new List<float>();
        
        for (int i = 0; i < Torque.Length; i++)
        {
            torqueList.Add(Torque[i]);
        }

        return torqueList;
    }

    /// <summary>
    /// ハンドリング情報を返す
    /// </summary>
    /// <returns>x=高速時の回転速度,y=低速時の回転速度</returns>
    public Vector2 GetHandling()
    {
        return new Vector2(slowSteerMoveSpeed, fastSteerMoveSpeed);
        
    }

    #endregion ### Getter ###

    #endregion ### Methods ###
}
