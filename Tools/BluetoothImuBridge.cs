using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

internal static class BluetoothImuBridge
{
    private const string DeviceName = "GAIT-SHANK";
    private static readonly Guid ServiceId = new Guid("19B30000-E8F2-537E-4F6C-D104768A1214");
    private static readonly Guid CharacteristicId = new Guid("19B30001-E8F2-537E-4F6C-D104768A1214");

    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--self-test")
        {
            var packet = new byte[18];
            packet[0] = 0x34;
            packet[1] = 0x12;
            System.Buffer.BlockCopy(BitConverter.GetBytes(.5f), 0, packet, 2, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(-.5f), 0, packet, 6, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(.5f), 0, packet, 10, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(-.5f), 0, packet, 14, 4);
            return Decode(packet) == "IMU,GAIT-SHANK,4660,0.5,-0.5,0.5,-0.5" && Decode(new byte[17]) == null ? 0 : 1;
        }
        try { Receive().GetAwaiter().GetResult(); return 0; }
        catch (Exception error) when (error.HResult == unchecked((int)0x800710DF))
        {
            Console.WriteLine("ERROR Windows Bluetooth is not ready. Turn on Bluetooth and retry.");
            return 1;
        }
        catch (Exception error) { Console.WriteLine("ERROR " + error.Message.Replace('\n', ' ').Replace('\r', ' ')); return 1; }
    }

    private static async Task Receive()
    {
        Console.WriteLine("STATUS Scanning for GAIT-SHANK...");
        var addresses = new HashSet<ulong>();
        var scanError = new TaskCompletionSource<BluetoothError>();
        var watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
        watcher.Received += (sender, args) =>
        {
            if (args.Advertisement.LocalName == DeviceName)
                lock (addresses) addresses.Add(args.BluetoothAddress);
        };
        watcher.Stopped += (sender, args) => scanError.TrySetResult(args.Error);
        watcher.Start();
        var scanDuration = Task.Delay(5000);
        await Task.WhenAny(scanDuration, scanError.Task);
        watcher.Stop();
        if (scanError.Task.IsCompleted && scanError.Task.Result != BluetoothError.Success)
        {
            if (scanError.Task.Result == BluetoothError.DisabledByUser)
                throw new InvalidOperationException("Bluetooth is disabled in Windows. Turn it on and retry.");
            throw new InvalidOperationException("Bluetooth scan failed: " + scanError.Task.Result);
        }

        ulong address;
        lock (addresses)
        {
            if (addresses.Count == 0) throw new InvalidOperationException("GAIT-SHANK was not found. Check that the shin sensor is on and advertising.");
            if (addresses.Count > 1) throw new InvalidOperationException("More than one GAIT-SHANK found. Leave only your shin sensor on and retry.");
            address = addresses.Single();
        }

        Console.WriteLine("STATUS Connecting to GAIT-SHANK...");
        using (var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address))
        {
            if (device == null) throw new InvalidOperationException("Windows could not open GAIT-SHANK.");
            var services = await device.GetGattServicesForUuidAsync(ServiceId, BluetoothCacheMode.Uncached);
            if (services.Status != GattCommunicationStatus.Success || services.Services.Count == 0)
                throw new InvalidOperationException("The sensor IMU service is unavailable: " + services.Status);
            using (var service = services.Services[0])
            {
                var characteristics = await service.GetCharacteristicsForUuidAsync(CharacteristicId, BluetoothCacheMode.Uncached);
                if (characteristics.Status != GattCommunicationStatus.Success || characteristics.Characteristics.Count == 0)
                    throw new InvalidOperationException("The sensor quaternion characteristic is unavailable: " + characteristics.Status);
                var characteristic = characteristics.Characteristics[0];
                characteristic.ValueChanged += OnPacket;
                try
                {
                    var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (status != GattCommunicationStatus.Success)
                        throw new InvalidOperationException("Could not subscribe to sensor readings: " + status);
                    Console.WriteLine("READY");
                    await Console.In.ReadLineAsync();
                }
                finally { characteristic.ValueChanged -= OnPacket; }
            }
        }
    }

    private static void OnPacket(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        if (args.CharacteristicValue.Length != 18) return;
        using (var reader = DataReader.FromBuffer(args.CharacteristicValue))
        {
            var bytes = new byte[18];
            reader.ReadBytes(bytes);
            Console.WriteLine(Decode(bytes));
        }
    }

    private static string Decode(byte[] bytes)
    {
        if (bytes.Length != 18) return null;
        return string.Format(CultureInfo.InvariantCulture, "IMU,GAIT-SHANK,{0},{1:R},{2:R},{3:R},{4:R}",
            BitConverter.ToUInt16(bytes, 0), BitConverter.ToSingle(bytes, 2), BitConverter.ToSingle(bytes, 6),
            BitConverter.ToSingle(bytes, 10), BitConverter.ToSingle(bytes, 14));
    }
}
