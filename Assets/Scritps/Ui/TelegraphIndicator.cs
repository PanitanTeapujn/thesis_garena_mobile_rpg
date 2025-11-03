using UnityEngine;

/// <summary>
/// Telegraph Indicator แบบ Advanced พร้อม Animation และเอฟเฟกต์พิเศษ
/// ใช้ script นี้สำหรับสร้าง Prefab ที่สวยงามกว่าแบบ default
/// </summary>
public class TelegraphIndicator : MonoBehaviour
{
    [Header("📐 Shape Settings")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private ParticleSystem edgeParticles;  // Particles รอบขอบ
    [SerializeField] private Light areaLight;                // แสงในพื้นที่
    
    [Header("🎨 Visual Settings")]
    [SerializeField] private Color startColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private Color endColor = new Color(1f, 0f, 0f, 0.8f);
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float pulseSpeed = 3f;
    
    [Header("⚡ Animation Settings")]
    [SerializeField] private bool rotateIndicator = true;
    [SerializeField] private float rotationSpeed = 30f;      // องศาต่อวินาที
    [SerializeField] private bool scaleAnimation = true;
    [SerializeField] private float scaleAmplitude = 0.1f;
    
    [Header("🔊 Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip warningSound;
    [SerializeField] private AudioClip urgentSound;          // เสียงเมื่อใกล้หมดเวลา
    
    private Material indicatorMaterial;
    private float startTime;
    private float duration;
    private Vector3 initialScale;
    
    public void Initialize(float durationSeconds, Color color)
    {
        startTime = Time.time;
        duration = durationSeconds;
        startColor = new Color(color.r, color.g, color.b, 0.3f);
        endColor = new Color(color.r, color.g, color.b, 0.8f);
        initialScale = transform.localScale;
        
        SetupMaterial();
        SetupParticles();
        SetupLight();
        PlayWarningSound();
    }
    
    private void SetupMaterial()
    {
        if (meshRenderer != null)
        {
            indicatorMaterial = meshRenderer.material;
            indicatorMaterial.color = startColor;
            
            // เปิดใช้งาน Emission
            indicatorMaterial.EnableKeyword("_EMISSION");
            indicatorMaterial.SetColor("_EmissionColor", startColor * 0.5f);
        }
    }
    
    private void SetupParticles()
    {
        if (edgeParticles != null)
        {
            var main = edgeParticles.main;
            main.startColor = startColor;
            edgeParticles.Play();
        }
    }
    
    private void SetupLight()
    {
        if (areaLight != null)
        {
            areaLight.color = startColor;
            areaLight.intensity = 0.5f;
            areaLight.range = transform.localScale.x;
        }
    }
    
    private void PlayWarningSound()
    {
        if (audioSource != null && warningSound != null)
        {
            audioSource.PlayOneShot(warningSound);
        }
    }
    
    private void Update()
    {
        float elapsed = Time.time - startTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        
        // Color animation - เข้มขึ้นเรื่อยๆ
        AnimateColor(progress);
        
        // Rotation animation
        if (rotateIndicator)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        
        // Scale animation
        if (scaleAnimation)
        {
            AnimateScale(progress);
        }
        
        // Update light
        UpdateLight(progress);
        
        // Urgent warning ที่ 75% เวลา
        if (progress >= 0.75f && urgentSound != null && audioSource != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(urgentSound);
        }
    }
    
    private void AnimateColor(float progress)
    {
        if (indicatorMaterial == null) return;
        
        // กระพริบเร็วขึ้นตาม progress
        float pulseSpeed = Mathf.Lerp(2f, 10f, progress);
        float pulse = pulseCurve.Evaluate(Mathf.PingPong(Time.time * pulseSpeed, 1f));
        
        Color currentColor = Color.Lerp(startColor, endColor, progress);
        currentColor.a *= pulse;
        
        indicatorMaterial.color = currentColor;
        indicatorMaterial.SetColor("_EmissionColor", currentColor * Mathf.Lerp(0.5f, 2f, progress));
    }
    
    private void AnimateScale(float progress)
    {
        // Breathing effect
        float breatheSpeed = Mathf.Lerp(1f, 4f, progress);
        float scale = 1f + Mathf.Sin(Time.time * breatheSpeed) * scaleAmplitude * progress;
        transform.localScale = initialScale * scale;
    }
    
    private void UpdateLight(float progress)
    {
        if (areaLight == null) return;
        
        areaLight.intensity = Mathf.Lerp(0.5f, 2f, progress);
        areaLight.color = Color.Lerp(startColor, endColor, progress);
    }
    
    private void OnDestroy()
    {
        // Cleanup
        if (indicatorMaterial != null)
        {
            Destroy(indicatorMaterial);
        }
    }
}

/*
╔══════════════════════════════════════════════════════════════════════════════╗
║                    🎨 TELEGRAPH INDICATOR PREFAB SETUP GUIDE                  ║
╚══════════════════════════════════════════════════════════════════════════════╝

📋 วิธีสร้าง Telegraph Indicator Prefab:

1. สร้าง GameObject ใหม่ชื่อ "TelegraphIndicator"
   
2. เพิ่ม Components:
   ├── Mesh Filter (Cylinder หรือ Plane)
   ├── Mesh Renderer
   ├── Particle System (สำหรับขอบวงกลม)
   ├── Light (Point Light หรือ Area Light)
   └── TelegraphIndicator Script (script นี้)

3. ตั้งค่า Mesh:
   - Scale: (1, 0.05, 1) สำหรับ Cylinder
   - Rotation: (0, 0, 0)
   - Position: (0, 0.1, 0) - ยกขึ้นเล็กน้อยเพื่อไม่ให้ชนพื้น

4. สร้าง Material:
   - Shader: Standard
   - Rendering Mode: Transparent
   - Color: สีแดง (1, 0, 0, 0.5)
   - Emission: เปิด, สีแดง
   - ลาก Material ใส่ Mesh Renderer

5. ตั้งค่า Particle System:
   - Shape: Circle, Edge mode
   - Emission: Rate over Time = 50
   - Start Color: สีแดง (alpha 0.8)
   - Start Size: 0.1 - 0.3
   - Start Lifetime: 1 - 2
   - Start Speed: 0 - 1
   - Renderer: Billboard

6. ตั้งค่า Light:
   - Type: Point Light
   - Color: สีแดง
   - Range: 5
   - Intensity: 1.5

7. ตั้งค่า TelegraphIndicator Script:
   - Mesh Renderer: ลาก Mesh Renderer มาใส่
   - Edge Particles: ลาก Particle System มาใส่
   - Area Light: ลาก Light มาใส่
   - Start Color: (1, 0, 0, 0.3)
   - End Color: (1, 0, 0, 0.8)
   - Pulse Speed: 3
   - Rotation Speed: 30
   - Scale Amplitude: 0.1

8. (Optional) เพิ่มเสียง:
   - เพิ่ม Audio Source component
   - ใส่ Warning Sound Clip
   - ใส่ Urgent Sound Clip (เล่นเมื่อใกล้หมดเวลา)

9. บันทึกเป็น Prefab:
   - ลาก GameObject ไปที่ Project folder
   - ตั้งชื่อ "TelegraphIndicator_Fire" หรือ "TelegraphIndicator_Explosion"

10. นำไปใช้กับ FireSlime:
    - เปิด FireSlime prefab
    - ลาก TelegraphIndicator prefab ใส่ช่อง "Telegraph Indicator Prefab"
    - ตั้งค่า Telegraph Color และ Explosion Telegraph Color

╔══════════════════════════════════════════════════════════════════════════════╗
║                         🎨 ADVANCED CUSTOMIZATION                             ║
╚══════════════════════════════════════════════════════════════════════════════╝

💎 สำหรับผลลัพธ์ที่ดีกว่า:

1. ใช้ Shader Graph:
   - สร้าง animated texture ที่หมุน
   - เพิ่ม distortion effect
   - Glow และ pulse ที่ซับซ้อนกว่า

2. เพิ่ม Decal:
   - ใช้ Decal Projector สำหรับพื้น
   - แสดง texture ที่คมชัดกว่า

3. Particle Effects:
   - เพิ่ม sparks จากขอบ
   - เพิ่ม smoke หรือ heat distortion
   - Radial burst เมื่อใกล้หมดเวลา

4. Camera Shake:
   - เพิ่ม CameraShake เมื่อใกล้หมดเวลา
   - ทำให้รู้สึกถึงความเร่งด่วน

5. UI Warning:
   - แสดง UI warning icon เมื่ออยู่ในพื้นที่
   - Distance indicator

╔══════════════════════════════════════════════════════════════════════════════╗
║                              ⚠️ IMPORTANT NOTES                               ║
╚══════════════════════════════════════════════════════════════════════════════╝

⚡ Performance Tips:
- ใช้ simple shader สำหรับ mobile
- Disable shadow casting
- ใช้ particle count ที่เหมาะสม
- Cache material references

🔧 Debugging:
- ตรวจสอบว่า Collider ถูกลบแล้ว (ไม่ต้องการให้ชนได้)
- ตรวจสอบ Layer เพื่อไม่ให้บัง UI
- ใช้ Gizmos เพื่อดู radius

🎮 Player Experience:
- ต้องมองเห็นชัดเจนในทุกสภาพแสง
- สีต้อง contrast กับพื้นเกม
- Animation ต้องไม่ปวดตา
- เสียงต้องไม่รบกวนเกินไป

╔══════════════════════════════════════════════════════════════════════════════╗
║                            📦 READY-MADE EXAMPLES                             ║
╚══════════════════════════════════════════════════════════════════════════════╝

หากต้องการใช้แบบง่ายที่สุด:
❌ ไม่ต้องสร้าง Prefab
✅ FireSlime จะสร้าง procedural indicator อัตโนมัติ
✅ ใช้งานได้ทันทีโดยไม่ต้องตั้งค่าอะไร

หากต้องการความสวยงาม:
✅ สร้าง Prefab ตามคู่มือด้านบน
✅ ลากใส่ Inspector
✅ ปรับแต่งสีและ timing

*/
