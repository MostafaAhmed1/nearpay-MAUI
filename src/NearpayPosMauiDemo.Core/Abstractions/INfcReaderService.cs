using NearpayPosMauiDemo.Core.Models;

namespace NearpayPosMauiDemo.Core.Abstractions;

public interface INfcReaderService
{
    bool IsSupported { get; }
    bool IsListening { get; }

    event EventHandler<NfcTagDiscoveredEventArgs>? TagDiscovered;

    /// <summary>
    /// يبدأ وضع الاستماع لـ NFC عبر Foreground Dispatch (Android).
    /// ملاحظة: في وضع الاستماع، قد يتم اعتراض الـ NFC من التطبيق بدل خدمات أخرى؛
    /// لذلك يُفضّل إيقافه قبل تشغيل تدفق الدفع عبر NearPay إذا لزم.
    /// </summary>
    Task StartAsync(CancellationToken ct = default);

    Task StopAsync(CancellationToken ct = default);
}

