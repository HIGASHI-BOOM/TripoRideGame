using UnityEngine;

[CreateAssetMenu(fileName = "KartDriveConfig", menuName = "Kart/Drive Config")]
public sealed class KartDriveConfig : ScriptableObject
{
    [Header("Body")]
    [Tooltip("车辆刚体质量，数值越高越重，受加速、碰撞和惯性影响越明显。")]
    [Min(1f)] public float mass = 420f;
    [Tooltip("车辆重心位置，本地坐标；Y 值越低越不容易翻车，Z 值会影响前后重量分布。")]
    public Vector3 centerOfMass = new Vector3(0f, -0.45f, 0.05f);
    [Tooltip("线性阻力，会持续消耗速度；数值越高越难跑到极速。")]
    [Min(0f)] public float linearDrag = 0.12f;
    [Tooltip("角阻力，会抑制车身旋转；数值越高车身越不容易甩动。")]
    [Min(0f)] public float angularDrag = 2.8f;

    [Header("Power")]
    [Tooltip("前进加速力，数值越高起步和中段提速越快，但不会突破最大前进速度。")]
    [Min(0f)] public float accelerationForce = 20000f;
    [Tooltip("倒车加速力，数值越高倒车提速越快，但不会突破最大倒车速度。")]
    [Min(0f)] public float reverseForce = 8000f;
    [Tooltip("最大前进速度，单位是米/秒；41.6667 约等于 150 km/h。")]
    [Min(1f)] public float maxSpeed = 41.666668f;
    [Tooltip("最大倒车速度，单位是米/秒；13.8889 约等于 50 km/h。")]
    [Min(1f)] public float maxReverseSpeed = 13.888889f;
    [Tooltip("刹车力，数值越高松油门或刹车时减速越强。")]
    [Min(0f)] public float brakeForce = 8500f;

    [Header("Steering")]
    [Tooltip("低速转向角速度，数值越高低速时车头转得越快。")]
    [Min(1f)] public float lowSpeedTurnRate = 120f;
    [Tooltip("高速转向角速度，数值越高高速时仍然能更快转向。")]
    [Min(1f)] public float highSpeedTurnRate = 58f;
    [Tooltip("倒车时的转向倍率，数值越低倒车转向越慢、越稳。")]
    [Range(0.1f, 1f)] public float reverseSteerScale = 0.45f;

    [Header("Drift")]
    [Tooltip("最低入漂速度，车速低于这个值时按漂移键不会进入漂移。")]
    [Min(0f)] public float driftMinSpeed = 8f;
    [Tooltip("入漂需要的方向输入强度，数值越高越需要明显打方向才会开始漂移。")]
    [Range(0.05f, 1f)] public float driftSteerThreshold = 0.28f;
    [Tooltip("漂移时的横向抓地比例，数值越低越滑，数值越高越稳。")]
    [Range(0.1f, 1f)] public float driftGripScale = 0.62f;
    [Tooltip("漂移中顺着漂移方向打方向时的转向倍率，数值越高车头甩得越快。")]
    [Range(1f, 2.5f)] public float driftTurnMultiplier = 1.18f;
    [Tooltip("漂移中反打方向时的转向倍率，数值越高越容易用反打稳住车身。")]
    [Range(0.1f, 1.5f)] public float driftCounterSteerMultiplier = 0.95f;
    [Tooltip("入漂瞬间给车身的横向甩尾力度，数值太高会一按就横着飞。")]
    [Min(0f)] public float driftEntrySideImpulse = 1.4f;
    [Tooltip("入漂瞬间给车身的旋转冲量，数值越高车头越容易快速甩开。")]
    [Min(0f)] public float driftEntryYawKick = 3f;
    [Tooltip("松开漂移后的短暂回抓地倍率，数值越高出漂时越快收住侧滑。")]
    [Range(1f, 3f)] public float driftRecoveryGripMultiplier = 1.15f;
    [Tooltip("出漂回抓地持续时间，数值越长越明显地把车身拉回稳定状态。")]
    [Min(0f)] public float driftRecoveryDuration = 0.25f;
    [Tooltip("触发出漂小喷需要保持漂移的最短时间，太短会让小喷过于容易触发。")]
    [Min(0f)] public float driftMiniBoostMinDuration = 0.5f;
    [Tooltip("出漂小喷增加的前向速度，数值越高松开漂移后的提速越强。")]
    [Min(0f)] public float driftMiniBoostSpeedGain = 2.5f;
    [Tooltip("出漂小喷持续时间，数值越长加速感越明显。")]
    [Min(0f)] public float driftMiniBoostDuration = 0.28f;
    [Tooltip("Enable or disable the visual FX played when a drift mini-boost is triggered. The mini-boost physics still runs when this is off.")]
    public bool enableDriftMiniBoostFx = true;
    [Tooltip("Enable or disable the exhaust smoke visual FX.")]
    public bool enableExhaustSmokeFx = true;
    [Tooltip("Show or hide the rider character mounted on the kart.")]
    public bool showRiderCharacter = true;

    [Header("Grip")]
    [Tooltip("横向抓地力，数值越高越不容易侧滑，数值越低越容易甩尾。")]
    [Min(0f)] public float lateralGrip = 10f;
    [Tooltip("随速度增加的空气下压力，会影响地面极速；如果只是想缩短腾空时间，优先调空中重力倍率。")]
    [Min(0f)] public float downforce = 0f;
    [Tooltip("空中重力倍率，只在赛车离地时生效；数值越高落地越快，不会像下压力一样按车速拖慢地面极速。")]
    [Min(1f)] public float airborneGravityMultiplier = 2f;
    [Tooltip("车身自动扶正力度，数值越高越不容易侧翻，但车身姿态会更硬。")]
    [Min(0f)] public float uprightStability = 0.35f;

    [Header("Boost")]
    [Tooltip("加速状态的速度参考倍率，用于加速手感计算；当前极速仍由最大速度字段限制。")]
    [Min(1f)] public float boostSpeedMultiplier = 1.35f;
    [Tooltip("Extra top-speed multiplier while boost is active. Normal driving still uses Max Speed; item boosts and drift mini-boosts can reach Max Speed multiplied by this value.")]
    [Min(1f)] public float boostMaxSpeedMultiplier = 3f;
    [Tooltip("How quickly extra boost speed returns to Max Speed after boost ends, in meters per second squared. Higher values make the speedometer and motion drop faster.")]
    [Min(0f)] public float boostOverspeedReturnRate = 35f;
    [Tooltip("加速状态额外推力，数值越高小喷或加速时提速越明显。")]
    [Min(0f)] public float boostForce = 9000f;
    [Tooltip("加速状态持续时间，单位是秒。")]
    [Min(0.1f)] public float boostDuration = 1.25f;

    [Header("Screen Speed Lines")]
    [Tooltip("是否启用屏幕速度线效果。关闭后不会显示屏幕边缘速度线。")]
    public bool enableScreenSpeedLines = true;
    [Tooltip("低于这个速度时速度线完全隐藏，单位 km/h。")]
    [Min(0f)] public float screenSpeedLineVisibleAtSpeedKph = 45f;
    [Tooltip("达到这个速度时速度线数量和强度接近最大，单位 km/h。")]
    [Min(1f)] public float screenSpeedLineFullAtSpeedKph = 150f;
    [Tooltip("速度变化到视觉强度的平滑时间，数值越小响应越快。")]
    [Min(0.01f)] public float screenSpeedLineIntensitySmoothTime = 0.12f;
    [Tooltip("刚出现时的最少速度线数量。")]
    [Min(0)] public int screenSpeedLineMinCount = 5;
    [Tooltip("满速时的最多速度线数量，速度越快越接近这个值。")]
    [Min(0)] public int screenSpeedLineMaxCount = 54;
    [Tooltip("低速时每条速度线的长度，单位为 Canvas 像素。")]
    [Min(1f)] public float screenSpeedLineMinLength = 180f;
    [Tooltip("高速时每条速度线的长度，单位为 Canvas 像素。")]
    [Min(1f)] public float screenSpeedLineMaxLength = 620f;
    [Tooltip("低速时速度线靠近屏幕边缘的宽度。")]
    [Min(0.1f)] public float screenSpeedLineMinWidth = 5f;
    [Tooltip("高速时速度线靠近屏幕边缘的宽度。")]
    [Min(0.1f)] public float screenSpeedLineMaxWidth = 24f;
    [Tooltip("屏幕中心保留的空白比例，避免速度线盖住车和道路焦点。")]
    [Range(0f, 1f)] public float screenSpeedLineCenterClearRadius = 0.34f;
    [Tooltip("整体线条颜色和最大透明度。")]
    public Color screenSpeedLineColor = new Color(1f, 1f, 1f, 0.82f);
    [Tooltip("线条向中心流动的速度。")]
    [Min(0f)] public float screenSpeedLineFlowSpeed = 1.35f;
    [Tooltip("开启后不读取真实车速，而是使用下面的预览速度，方便在 Play Mode 手动调速度线效果。")]
    public bool useScreenSpeedLinePreview;
    [Tooltip("手动预览速度，单位 km/h。适合填 80、120、150 来检查不同速度下的线条数量。")]
    [Min(0f)] public float screenSpeedLinePreviewSpeedKph = 150f;
}
