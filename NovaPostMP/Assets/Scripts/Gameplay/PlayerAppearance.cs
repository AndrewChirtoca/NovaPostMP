using Fusion;
using UnityEngine;

public class PlayerAppearance : NetworkBehaviour
{
    [SerializeField] private Renderer _bodyRenderer;

    [Networked, OnChangedRender(nameof(ApplyColor))]
    private Color BodyColor { get; set; } = Color.white;

    private MaterialPropertyBlock _properties;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        _properties = new MaterialPropertyBlock();
    }

    public override void Spawned()
    {
        ApplyColor();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !GetInput(out NetworkInputData data))
            return;

        if (data.Buttons.IsSet(PlayerButton.ChangeColor))
        {
            BodyColor = Random.ColorHSV(0f, 1f, 0.65f, 1f, 0.8f, 1f);
        }
    }

    private void ApplyColor()
    {
        if (_bodyRenderer == null)
            return;

        _bodyRenderer.GetPropertyBlock(_properties);
        _properties.SetColor(BaseColorId, BodyColor);
        _bodyRenderer.SetPropertyBlock(_properties);
    }
}
