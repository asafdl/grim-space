using Godot;
using GrimSpace.Application;

namespace GrimSpace.Presentation.Menu;

/// <summary>Focused sink while rebinding; swallows keys so they do not reach gameplay or Apply.</summary>
public partial class KeyCaptureControl : Control
{
	public event Action<KeyBinding, bool>? BindingCaptured;
	public event Action? CaptureCancelled;

	private StringName _action = "";
	private bool _secondarySlot;
	private Button? _restoreFocus;

	public void BeginCapture(StringName action, bool secondarySlot, Button restoreFocus)
	{
		_action = action;
		_secondarySlot = secondarySlot;
		_restoreFocus = restoreFocus;
		GrabFocus();
	}

	public void EndCapture()
	{
		_restoreFocus?.GrabFocus();
		_restoreFocus = null;
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false } keyEvent)
		{
			AcceptEvent();

			if (keyEvent.Keycode == Key.Escape)
			{
				CaptureCancelled?.Invoke();
				EndCapture();
				return;
			}

			if (_secondarySlot
				&& keyEvent.Keycode is Key.Backspace or Key.Delete)
			{
				BindingCaptured?.Invoke(KeyBinding.Empty, _secondarySlot);
				EndCapture();
				return;
			}

			if (!GameInputBindings.TryFindSpec(_action, out var spec)
				|| spec.CapturePolicy == BindingCapturePolicy.MouseOnly
				|| !GameInputBindings.TryCaptureKeyboard(keyEvent, out var binding)
				|| GameInputBindings.IsReserved(spec.Context, binding))
				return;

			BindingCaptured?.Invoke(binding, _secondarySlot);
			EndCapture();
			return;
		}

		if (@event is InputEventMouseButton mouseEvent)
		{
			AcceptEvent();

			if (!_secondarySlot && mouseEvent.ButtonIndex == MouseButton.Right && !mouseEvent.Pressed)
			{
				CaptureCancelled?.Invoke();
				EndCapture();
				return;
			}

			if (_secondarySlot
				&& mouseEvent.Pressed
				&& mouseEvent.ButtonIndex == MouseButton.Right)
			{
				BindingCaptured?.Invoke(KeyBinding.Empty, _secondarySlot);
				EndCapture();
				return;
			}

			if (!GameInputBindings.TryFindSpec(_action, out var spec)
				|| spec.CapturePolicy == BindingCapturePolicy.KeyboardOnly
				|| !GameInputBindings.TryCaptureMouse(mouseEvent, out var binding))
				return;

			BindingCaptured?.Invoke(binding, _secondarySlot);
			EndCapture();
		}
	}
}
