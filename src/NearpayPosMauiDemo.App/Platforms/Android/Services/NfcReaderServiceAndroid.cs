using Android.App;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Java.Util;
using NearpayPosMauiDemo.Core.Abstractions;
using NearpayPosMauiDemo.Core.Models;
using Microsoft.Maui.ApplicationModel;

namespace NearpayPosMauiDemo.App.Platforms.Android.Services;

public sealed class NfcReaderServiceAndroid : Java.Lang.Object, INfcReaderService, IDisposable
{
    private NfcAdapter? _adapter;
    private PendingIntent? _pendingIntent;
    private IntentFilter[]? _intentFilters;
    private string[][]? _techLists;

    public bool IsSupported => _adapter is not null;
    public bool IsListening { get; private set; }

    public event EventHandler<NfcTagDiscoveredEventArgs>? TagDiscovered;

    public NfcReaderServiceAndroid()
    {
        var context = global::Android.App.Application.Context;
        _adapter = NfcAdapter.GetDefaultAdapter(context);

        // إعدادات intent للفورجراوند ديسباتش
        _intentFilters = new[]
        {
            new IntentFilter(NfcAdapter.ActionTagDiscovered),
            new IntentFilter(NfcAdapter.ActionTechDiscovered),
            new IntentFilter(NfcAdapter.ActionNdefDiscovered),
        };

        // غالباً بطاقات الدفع تكون IsoDep
        _techLists = new[]
        {
            new[] { Java.Lang.Class.FromType(typeof(IsoDep)).Name },
            new[] { Java.Lang.Class.FromType(typeof(Ndef)).Name },
            new[] { Java.Lang.Class.FromType(typeof(NdefFormatable)).Name }
        };

        MainActivity.NfcIntentReceived += OnNfcIntent;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_adapter is null)
            return Task.CompletedTask;

        var activity = Platform.CurrentActivity;
        if (activity is null)
            throw new InvalidOperationException("لا يوجد Activity حالي لتفعيل NFC foreground dispatch.");

        var intent = new Intent(activity, activity.Class);
        intent.AddFlags(ActivityFlags.SingleTop);

        _pendingIntent = PendingIntent.GetActivity(
            activity,
            requestCode: 0,
            intent,
            GetPendingIntentFlags());

        _adapter.EnableForegroundDispatch(activity, _pendingIntent, _intentFilters, _techLists);
        IsListening = true;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_adapter is null)
            return Task.CompletedTask;

        var activity = Platform.CurrentActivity;
        if (activity is null)
            return Task.CompletedTask;

        try
        {
            _adapter.DisableForegroundDispatch(activity);
        }
        catch
        {
            // ignore
        }

        IsListening = false;
        return Task.CompletedTask;
    }

    private void OnNfcIntent(Intent intent)
    {
        if (!IsListening)
            return;

        var action = intent.Action;
        if (action != NfcAdapter.ActionTagDiscovered &&
            action != NfcAdapter.ActionTechDiscovered &&
            action != NfcAdapter.ActionNdefDiscovered)
            return;

        var tag = (Tag?)intent.GetParcelableExtra(NfcAdapter.ExtraTag);
        if (tag is null)
            return;

        var idHex = tag.GetId() is byte[] idBytes ? BitConverter.ToString(idBytes).Replace("-", string.Empty) : null;
        var techs = tag.GetTechList()?.ToList() ?? new List<string>();

        string? ndefText = null;
        try
        {
            var ndef = Ndef.Get(tag);
            var message = ndef?.CachedNdefMessage;
            var records = message?.GetRecords();
            if (records is { Length: > 0 })
            {
                // أفضل محاولة: قراءة أول record كنص إن كان NDEF Text
                var record = records[0];
                var payload = record.GetPayload();
                if (payload is { Length: > 3 })
                {
                    // NDEF Text: payload[0] status, then language code, then text
                    var langLen = payload[0] & 0x3F;
                    var textBytes = payload.Skip(1 + langLen).ToArray();
                    ndefText = System.Text.Encoding.UTF8.GetString(textBytes);
                }
            }
        }
        catch
        {
            // غالباً بطاقات الدفع لا تحتوي NDEF
        }

        var info = new NfcTagInfo(idHex, techs, ndefText);
        TagDiscovered?.Invoke(this, new NfcTagDiscoveredEventArgs(info));
    }

    private static PendingIntentFlags GetPendingIntentFlags()
    {
        // Immutable works fine for foreground dispatch and avoids API 31+ mutable requirement.
        return PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            MainActivity.NfcIntentReceived -= OnNfcIntent;
        }
        base.Dispose(disposing);
    }
}
