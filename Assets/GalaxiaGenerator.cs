using UnityEngine;

public class GalaxiaGenerator : MonoBehaviour
{
    [Header("Forma")]
    public float raio = 12f;
    public int numBracos = 2;
    public float torcao = 3.5f;          // quanto os braços enrolam
    public float espessura = 0.6f;       // espessura do disco

    [Header("Quantidade")]
    public int numEstrelas = 8000;
    public int numGas = 250;
    public int numNucleo = 150;

    [Header("Movimento")]
    public float velocidadeRotacao = 8f; // graus por segundo
    public Vector3 inclinacao = new Vector3(25f, 0f, 15f);

    [Header("Cores")]
    public Color corNucleo = new Color(1f, 0.75f, 0.4f);
    public Color corBracoInterno = new Color(0.9f, 0.9f, 1f);
    public Color corBracoExterno = new Color(0.35f, 0.55f, 1f);
    public Color corGas1 = new Color(0.3f, 0.4f, 1f);
    public Color corGas2 = new Color(0.8f, 0.3f, 0.8f);

    [Header("Opcional")]
    public Material materialCustom;

    Transform disco;
    Material mat;

    void Start()
    {
        transform.rotation = Quaternion.Euler(inclinacao);

        // Objeto filho que gira; o pai fica inclinado
        disco = new GameObject("Disco").transform;
        disco.SetParent(transform, false);

        mat = materialCustom != null ? materialCustom : CriarMaterial();

        CriarEstrelas();
        CriarGas();
        CriarNucleo();
        CriarFundo();
    }
    [Header("Fundo")]
    public float raioFundo = 150f;
    public float taxaFundo = 700f;

    void CriarFundo()
    {
        var go = new GameObject("EstrelasFundo");
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 5000;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.92f, 1f, 1f), Color.white);

        var em = ps.emission;
        em.rateOverTime = taxaFundo;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = raioFundo;
        sh.radiusThickness = 0.1f;   // fica só perto da superfície da esfera

        // Piscar: aparece e some suavemente
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material = mat;

        ps.Play();
    }

    void Update()
    {
        disco.Rotate(0f, velocidadeRotacao * Time.deltaTime, 0f, Space.Self);
    }

    // ---------- Sistemas ----------

    void CriarEstrelas()
    {
        var ps = NovoSistema("Estrelas", numEstrelas);
        var p = new ParticleSystem.EmitParams();

        for (int i = 0; i < numEstrelas; i++)
        {
            float t = Mathf.Pow(Random.value, 0.7f);        // mais estrelas perto do centro
            float r = t * raio;
            float ang = AnguloBraco(r) + Gauss() * Mathf.Lerp(0.15f, 0.35f, t);

            p.position = new Vector3(Mathf.Cos(ang) * r,
                                     Gauss() * espessura * (1f - t * 0.7f),
                                     Mathf.Sin(ang) * r);
            p.startSize = Random.Range(0.05f, 0.18f);
            Color c = Color.Lerp(corBracoInterno, corBracoExterno, t);
            c.a = Random.Range(0.5f, 1f);
            p.startColor = c;
            p.startLifetime = 1e6f;
            ps.Emit(p, 1);
        }
    }

    [Header("Gas")]
    [Range(0f, 1f)] public float gasNoDisco = 0.4f;   // parte do gás que preenche o disco todo
    public float espalhamentoGas = 0.6f;              // quanto o gás se afasta do braço

    void CriarGas()
    {
        var ps = NovoSistema("Gas", numGas);
        var p = new ParticleSystem.EmitParams();

        for (int i = 0; i < numGas; i++)
        {
            float t = Random.Range(0.1f, 1f);
            float r = t * raio;
            float ang;

            if (Random.value < gasNoDisco)
                ang = Random.Range(0f, Mathf.PI * 2f);              // qualquer lugar do disco
            else
                ang = AnguloBraco(r) + Gauss() * espalhamentoGas;   // perto dos braços, bem espalhado

            p.position = new Vector3(Mathf.Cos(ang) * r,
                Gauss() * espessura * 0.5f,
                Mathf.Sin(ang) * r);
            p.startSize = Random.Range(3f, 7f) * (0.6f + t);
            Color c = Color.Lerp(corGas1, corGas2, Random.value);
            c.a = Random.Range(0.01f, 0.03f);
            p.startColor = c;
            p.rotation3D = new Vector3(0, 0, Random.Range(0f, 360f));
            p.startLifetime = 1e6f;
            ps.Emit(p, 1);
        }
    }

    void CriarNucleo()
    {
        var ps = NovoSistema("Nucleo", numNucleo);
        var p = new ParticleSystem.EmitParams();

        for (int i = 0; i < numNucleo; i++)
        {
            p.position = new Vector3(Gauss() * 1.2f, Gauss() * 0.4f, Gauss() * 1.2f);
            p.startSize = Random.Range(2f, 4f);
            Color c = Color.Lerp(corNucleo, Color.white, Random.value * 0.6f);
            c.a = Random.Range(0.03f, 0.07f);
            p.startColor = c;
            p.startLifetime = 1e6f;
            ps.Emit(p, 1);
        }
    }

    // ---------- Auxiliares ----------

    // Espiral logarítmica: cada braço tem um ângulo base diferente
    float AnguloBraco(float r)
    {
        int braco = Random.Range(0, numBracos);
        float baseAng = braco * Mathf.PI * 2f / numBracos;
        return baseAng + (r / raio) * torcao;
    }

    ParticleSystem NovoSistema(string nome, int max)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(disco, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = max;
        main.startSpeed = 0f;

        var em = ps.emission;
        em.enabled = false;          // só emitimos pelo código

        var sh = ps.shape;
        sh.enabled = false;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material = mat;

        ps.Play();
        return ps;
    }

    // Gaussiana aproximada (média e desvio ~1)
    float Gauss()
    {
        return (Random.value + Random.value + Random.value + Random.value - 2f) * 1.7f;
    }

    Material CriarMaterial()
    {
        Shader sh = Shader.Find("Legacy Shaders/Particles/Additive");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");

        var m = new Material(sh);
        var tex = CriarTexturaRedonda(64);

        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);

        // Se caiu no shader URP, configura transparente + aditivo
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1);   // Transparent
            m.SetFloat("_Blend", 2);     // Additive
            m.SetFloat("_SrcBlend", 1);
            m.SetFloat("_DstBlend", 1);
            m.SetFloat("_ZWrite", 0);
            m.renderQueue = 3000;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        return m;
    }

    // Textura de círculo com borda suave (feita por código)
    Texture2D CriarTexturaRedonda(int tam)
    {
        var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (tam - 1) / 2f;
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        return tex;
    }
}