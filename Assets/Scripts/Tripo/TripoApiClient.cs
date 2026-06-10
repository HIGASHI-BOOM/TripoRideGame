using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed class TripoApiClient
{
    private const string OpenApiBaseUrl = "https://openapi.tripo3d.com";
    private const string TextToModelEndpoint = OpenApiBaseUrl + "/v3/generation/text-to-model";
    private const string RigCheckEndpoint = OpenApiBaseUrl + "/v3/animations/rig-check";
    private const string RigEndpoint = OpenApiBaseUrl + "/v3/animations/rig";
    private const string RetargetEndpoint = OpenApiBaseUrl + "/v3/animations/retarget";
    private const string ConvertModelEndpoint = OpenApiBaseUrl + "/v3/models/convert";
    private const string TaskStatusEndpoint = OpenApiBaseUrl + "/v3/tasks";

    private readonly string apiKey;
    private readonly float pollIntervalSeconds;
    private readonly int timeoutSeconds;

    public TripoApiClient(string apiKey, float pollIntervalSeconds, int timeoutSeconds)
    {
        this.apiKey = apiKey;
        this.pollIntervalSeconds = Mathf.Max(1f, pollIntervalSeconds);
        this.timeoutSeconds = Mathf.Max(30, timeoutSeconds);
    }

    public IEnumerator GenerateModelFromText(
        TripoTextToModelRequest request,
        bool convertToFbx,
        Action<TripoTaskData> onProgress,
        Action<TripoTaskData> onSuccess,
        Action<string> onFailure)
    {
        TripoTaskData modelTask = null;
        yield return SubmitTask(
            TextToModelEndpoint,
            request.ToJson(),
            data => modelTask = data,
            onFailure);

        if (modelTask == null)
        {
            yield break;
        }

        yield return WaitForTask(modelTask.task_id, onProgress, data => modelTask = data, onFailure);
        if (modelTask == null || !modelTask.IsSuccess)
        {
            yield break;
        }

        if (!convertToFbx)
        {
            onSuccess?.Invoke(modelTask);
            yield break;
        }

        TripoTaskData conversionTask = null;
        string conversionJson = TripoTaskJson.Object(
            ("input", TripoTaskJson.String(modelTask.task_id)),
            ("format", TripoTaskJson.String("FBX")),
            ("texture_format", TripoTaskJson.String("PNG")),
            ("pivot_to_center_bottom", TripoTaskJson.Bool(true)),
            ("with_animation", TripoTaskJson.Bool(false)));

        yield return SubmitTask(
            ConvertModelEndpoint,
            conversionJson,
            data => conversionTask = data,
            onFailure);

        if (conversionTask == null)
        {
            yield break;
        }

        yield return WaitForTask(conversionTask.task_id, onProgress, onSuccess, onFailure);
    }

    public IEnumerator GenerateAnimatedAnimalMountFromText(
        TripoTextToModelRequest request,
        TripoAnimalAnimationRequest animationRequest,
        Action<TripoTaskData> onProgress,
        Action<TripoTaskData> onSuccess,
        Action<string> onFailure)
    {
        request.texture = true;
        request.pbr = true;
        request.exportUv = true;

        TripoTaskData modelTask = null;
        yield return GenerateModelFromText(
            request,
            false,
            onProgress,
            data => modelTask = data,
            onFailure);

        if (modelTask == null || !modelTask.IsSuccess)
        {
            yield break;
        }

        string textureSourceModelUrl = null;
        TripoTaskData textureSourceTask = null;
        string textureSourceFailure = null;
        string textureSourceJson = TripoTaskJson.Object(
            ("input", TripoTaskJson.String(modelTask.task_id)),
            ("format", TripoTaskJson.String("FBX")),
            ("texture_format", TripoTaskJson.String("PNG")),
            ("pivot_to_center_bottom", TripoTaskJson.Bool(true)),
            ("with_animation", TripoTaskJson.Bool(false)));

        yield return SubmitTask(
            ConvertModelEndpoint,
            textureSourceJson,
            data => textureSourceTask = data,
            message => textureSourceFailure = message);

        if (textureSourceTask != null)
        {
            yield return WaitForTask(
                textureSourceTask.task_id,
                onProgress,
                data => textureSourceTask = data,
                message => textureSourceFailure = message);

            textureSourceModelUrl = textureSourceTask?.output?.BestModelUrl;
        }

        if (!string.IsNullOrWhiteSpace(textureSourceFailure))
        {
            Debug.LogWarning($"Tripo animal texture source conversion failed and will be skipped: {textureSourceFailure}");
        }

        TripoTaskData rigCheckTask = null;
        yield return SubmitTask(
            RigCheckEndpoint,
            TripoTaskJson.Object(("input", TripoTaskJson.String(modelTask.task_id))),
            data => rigCheckTask = data,
            onFailure);

        if (rigCheckTask == null)
        {
            yield break;
        }

        yield return WaitForTask(rigCheckTask.task_id, onProgress, data => rigCheckTask = data, onFailure);
        if (rigCheckTask == null || !rigCheckTask.IsSuccess)
        {
            yield break;
        }

        if (rigCheckTask.output == null || !rigCheckTask.output.riggable)
        {
            onFailure?.Invoke("Generated animal mount is not riggable. Try a clearer full-body side-view quadruped prompt.");
            yield break;
        }

        string rigType = !string.IsNullOrWhiteSpace(rigCheckTask.output.rig_type)
            ? rigCheckTask.output.rig_type
            : animationRequest.rigType;

        TripoTaskData rigTask = null;
        string rigJson = TripoTaskJson.Object(
            ("input", TripoTaskJson.String(modelTask.task_id)),
            ("model", TripoTaskJson.String(animationRequest.rigModel)),
            ("rig_type", TripoTaskJson.String(rigType)),
            ("spec", TripoTaskJson.String(animationRequest.spec)),
            ("out_format", TripoTaskJson.String(animationRequest.outFormat)));

        yield return SubmitTask(RigEndpoint, rigJson, data => rigTask = data, onFailure);
        if (rigTask == null)
        {
            yield break;
        }

        yield return WaitForTask(rigTask.task_id, onProgress, data => rigTask = data, onFailure);
        if (rigTask == null || !rigTask.IsSuccess)
        {
            yield break;
        }

        TripoTaskData retargetTask = null;
        string retargetJson = TripoTaskJson.Object(
            ("input", TripoTaskJson.String(rigTask.task_id)),
            ("animations", TripoTaskJson.StringArray(animationRequest.animation)),
            ("out_format", TripoTaskJson.String(animationRequest.outFormat)),
            ("bake_animation", TripoTaskJson.Bool(animationRequest.bakeAnimation)),
            ("export_with_geometry", TripoTaskJson.Bool(animationRequest.exportWithGeometry)),
            ("animate_in_place", TripoTaskJson.Bool(animationRequest.animateInPlace)));

        yield return SubmitTask(RetargetEndpoint, retargetJson, data => retargetTask = data, onFailure);
        if (retargetTask == null)
        {
            yield break;
        }

        yield return WaitForTask(retargetTask.task_id, onProgress, data => retargetTask = data, onFailure);
        if (retargetTask != null)
        {
            retargetTask.texture_source_model_url = textureSourceModelUrl;
            onSuccess?.Invoke(retargetTask);
        }
    }

    public IEnumerator DownloadBytes(string url, Action<byte[], string, string> onSuccess, Action<string> onFailure)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            onFailure?.Invoke("Tripo task completed without a model download URL.");
            yield break;
        }

        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.timeout = timeoutSeconds;
        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            onFailure?.Invoke($"Model download failed: {webRequest.error}");
            yield break;
        }

        string contentDisposition = webRequest.GetResponseHeader("Content-Disposition");
        string contentType = webRequest.GetResponseHeader("Content-Type");
        onSuccess?.Invoke(webRequest.downloadHandler.data, contentDisposition, contentType);
    }

    private IEnumerator SubmitTask(string endpoint, string json, Action<TripoTaskData> onSuccess, Action<string> onFailure)
    {
        using UnityWebRequest webRequest = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);
        webRequest.uploadHandler = new UploadHandlerRaw(body);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.timeout = timeoutSeconds;
        webRequest.SetRequestHeader("Content-Type", "application/json");
        webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            onFailure?.Invoke($"Tripo task submit failed: {webRequest.error}\n{webRequest.downloadHandler.text}");
            yield break;
        }

        TripoTaskEnvelope envelope = ParseEnvelope(webRequest.downloadHandler.text, onFailure);
        if (envelope?.data == null || string.IsNullOrWhiteSpace(envelope.data.task_id))
        {
            onFailure?.Invoke($"Tripo task submit returned no task_id:\n{webRequest.downloadHandler.text}");
            yield break;
        }

        onSuccess?.Invoke(envelope.data);
    }

    private IEnumerator WaitForTask(
        string taskId,
        Action<TripoTaskData> onProgress,
        Action<TripoTaskData> onSuccess,
        Action<string> onFailure)
    {
        while (true)
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get($"{TaskStatusEndpoint}/{taskId}");
            webRequest.timeout = timeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                onFailure?.Invoke($"Tripo task poll failed: {webRequest.error}\n{webRequest.downloadHandler.text}");
                yield break;
            }

            TripoTaskEnvelope envelope = ParseEnvelope(webRequest.downloadHandler.text, onFailure);
            TripoTaskData data = envelope?.data;
            if (data == null)
            {
                onFailure?.Invoke($"Tripo task poll returned no data:\n{webRequest.downloadHandler.text}");
                yield break;
            }

            onProgress?.Invoke(data);
            if (data.IsSuccess)
            {
                onSuccess?.Invoke(data);
                yield break;
            }

            if (data.IsFinalFailure)
            {
                onFailure?.Invoke($"Tripo task {taskId} ended with status '{data.status}'.");
                yield break;
            }

            yield return new WaitForSeconds(pollIntervalSeconds);
        }
    }

    private static TripoTaskEnvelope ParseEnvelope(string json, Action<string> onFailure)
    {
        try
        {
            return JsonUtility.FromJson<TripoTaskEnvelope>(json);
        }
        catch (Exception exception)
        {
            onFailure?.Invoke($"Could not parse Tripo response: {exception.Message}\n{json}");
            return null;
        }
    }
}

[Serializable]
public sealed class TripoTextToModelRequest
{
    public string prompt;
    public string negativePrompt;
    public string model = "P1-20260311";
    public int imageSeed = -1;
    public int modelSeed = -1;
    public int textureSeed = -1;
    public string textureQuality = "standard";
    public string compress;
    public int faceLimit = 3000;
    public bool texture = true;
    public bool pbr = true;
    public bool autoSize;
    public bool exportUv = true;

    public string ToJson()
    {
        TripoTaskJson.ObjectBuilder builder = TripoTaskJson.BeginObject();
        builder.Add("prompt", TripoTaskJson.String(prompt));
        builder.Add("model", TripoTaskJson.String(model));
        builder.Add("texture", TripoTaskJson.Bool(texture));
        builder.Add("pbr", TripoTaskJson.Bool(pbr));
        builder.Add("auto_size", TripoTaskJson.Bool(autoSize));
        builder.Add("export_uv", TripoTaskJson.Bool(exportUv));

        if (!string.IsNullOrWhiteSpace(negativePrompt))
        {
            builder.Add("negative_prompt", TripoTaskJson.String(negativePrompt));
        }

        if (!string.IsNullOrWhiteSpace(textureQuality))
        {
            builder.Add("texture_quality", TripoTaskJson.String(textureQuality));
        }

        if (faceLimit > 0)
        {
            builder.Add("face_limit", TripoTaskJson.Number(faceLimit));
        }

        if (imageSeed >= 0)
        {
            builder.Add("image_seed", TripoTaskJson.Number(imageSeed));
        }

        if (modelSeed >= 0)
        {
            builder.Add("model_seed", TripoTaskJson.Number(modelSeed));
        }

        if (textureSeed >= 0)
        {
            builder.Add("texture_seed", TripoTaskJson.Number(textureSeed));
        }

        if (!string.IsNullOrWhiteSpace(compress))
        {
            builder.Add("compress", TripoTaskJson.String(compress));
        }

        return builder.Build();
    }
}

[Serializable]
public sealed class TripoAnimalAnimationRequest
{
    public string rigModel = "v2.5-20260210";
    public string rigType = "quadruped";
    public string spec = "tripo";
    public string outFormat = "glb";
    public string animation = "preset:quadruped:walk";
    public bool bakeAnimation = true;
    public bool exportWithGeometry = true;
    public bool animateInPlace;
}

[Serializable]
public sealed class TripoTaskEnvelope
{
    public int code;
    public string message;
    public TripoTaskData data;
}

[Serializable]
public sealed class TripoTaskData
{
    public string task_id;
    public string type;
    public string status;
    public TripoTaskOutput output;
    public int progress;
    public int consumed_credit;
    public int credits_consumed;
    public int queuing_num;
    public int running_left_time;
    public string created_at;
    public string completed_at;
    public string texture_source_model_url;

    public bool IsSuccess => string.Equals(status, "success", StringComparison.OrdinalIgnoreCase);

    public bool IsFinalFailure =>
        string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "banned", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "expired", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "unknown", StringComparison.OrdinalIgnoreCase);
}

[Serializable]
public sealed class TripoTaskOutput
{
    public string model_url;
    public string[] model_urls;
    public string rendered_image_url;
    public bool riggable;
    public string rig_type;
    public string model;
    public string base_model;
    public string pbr_model;
    public string rendered_image;
    public string generated_image;

    public string BestModelUrl
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(model_url))
            {
                return model_url;
            }

            if (model_urls != null && model_urls.Length > 0 && !string.IsNullOrWhiteSpace(model_urls[0]))
            {
                return model_urls[0];
            }

            if (!string.IsNullOrWhiteSpace(pbr_model))
            {
                return pbr_model;
            }

            if (!string.IsNullOrWhiteSpace(model))
            {
                return model;
            }

            return base_model;
        }
    }
}

public static class TripoTaskJson
{
    public static ObjectBuilder BeginObject()
    {
        return new ObjectBuilder();
    }

    public static string Object(params (string key, string value)[] entries)
    {
        ObjectBuilder builder = BeginObject();
        for (int i = 0; i < entries.Length; i++)
        {
            builder.Add(entries[i].key, entries[i].value);
        }

        return builder.Build();
    }

    public static string String(string value)
    {
        return $"\"{Escape(value ?? string.Empty)}\"";
    }

    public static string Bool(bool value)
    {
        return value ? "true" : "false";
    }

    public static string Number(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public static string StringArray(params string[] values)
    {
        StringBuilder builder = new StringBuilder("[");
        for (int i = 0; values != null && i < values.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(String(values[i]));
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string Escape(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length + 8);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            switch (c)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (char.IsControl(c))
                    {
                        builder.Append("\\u");
                        builder.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    public sealed class ObjectBuilder
    {
        private readonly StringBuilder builder = new StringBuilder("{");
        private bool hasEntries;

        public void Add(string key, string value)
        {
            if (hasEntries)
            {
                builder.Append(',');
            }

            builder.Append(String(key));
            builder.Append(':');
            builder.Append(value);
            hasEntries = true;
        }

        public string Build()
        {
            builder.Append('}');
            return builder.ToString();
        }
    }
}
