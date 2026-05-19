# **eGPU Manager II for Windows**

A simple utility to solve stability issues with eGPU docks (like the Aoostar AG02 or others) on Windows, especially when the system enters sleep mode, standby, or the user session is locked.

This tool runs in the background and safely disables the eGPU device before these power state transitions occur, and re-enables it upon waking, preventing system freezes and crashes.

## **The Problem**

Many eGPU setups on Windows suffer from instability when the host computer attempts to enter a low-power state. This can happen when:

* The system goes to sleep/standby.  
* The display turns off after a period of inactivity.  
* The user locks their session (Win \+ L).

In these scenarios, the eGPU dock may not handle the power state transition correctly, leading to system hangs, freezes, or a "code 43" error in Device Manager upon resume, requiring a full reboot to fix.

## **Features**

* **Automatic Device Management**: Monitors system events for sleep, resume, session lock, and session unlock.  
* **Safe State Transitions**: Automatically disables the eGPU device before sleep/lock and re-enables it after resume/unlock.  
* **System Tray Icon**: Runs silently in the system tray with a visual indicator of its status (green for active, red for inactive).  
* **Simple UI**: A minimal interface to set the eGPU's InstanceId and view a real-time log of events.  
* **Persistent Configuration**: Remembers your InstanceId between sessions.  
* **Lightweight**: Built with C\# and Windows Forms, with minimal resource usage.
* **Auto Scan PCIID**: Search PCIID for eGPU parent. (II)
* **Persistent log**: Remembers your logs between sessions. (II)
* **Persistent PCIID**: Remembers your parent PCIID between sessions. (II)
* **Persistent windows size**: Keep windows size between launches. (II)
* **Show GFX information**: Display eGPU card information. (II) 
* **Run as a service**: Tool can be installed as Service from tray menu. (II) 


## **How to Compile and Use**

### **Prerequisites**

* Windows 10 or 11\.  
* **Visual Studio 2019 or later** (the [Community Edition](https://visualstudio.microsoft.com/vs/community/) is free). Make sure the **.NET desktop development** workload is installed.

### **Step 1: Find Your eGPU's InstanceId**

You need a unique identifier for your eGPU dock so the program knows which device to manage. **It is crucial to select the dock's root controller, not the GPU itself.** Here is the most reliable method:

1. Make sure your eGPU is connected and the external GPU is visible in Windows.  
2. Open **Device Manager** (right-click the Start Menu and select "Device Manager").  
3. Expand the **"Display adapters"** section. You should see both your internal and external GPU (e.g., NVIDIA GeForce RTX 4070).  
4. Right-click on your **external GPU**, select **Properties**, and go to the **Details** tab.  
5. From the "Property" dropdown menu, select **"Parent"**.  
6. The value shown in the box below is the InstanceId of the controller that the GPU is connected to. This is the ID you need. It will look something like PCI\\VEN\_174C\&DEV\_2461....  
7. **Right-click this value and copy it.** This is the ID you will paste into the eGPU Manager.

### **Step 2: Compile the Application**

1. Clone this repository or download the source code as a ZIP file.  
2. Open the .sln file in Visual Studio.  
3. Build the project by pressing F6 or selecting Build \> Build Solution from the menu. (If you encounter errors, see the Troubleshooting section below).  
4. The executable file (eGPUManager.exe) will be located in the bin\\Debug or bin\\Release folder inside the project directory.

### **Step 3: Run the Application**

1. **IMPORTANT**: The application needs to run with administrative privileges to manage hardware devices. Right-click on eGPUManager.exe and select **"Run as administrator"**.  
2. Paste the InstanceId you copied in Step 1 into the text box.  
3. Check the **"Enable Management"** box. The status icon in the system tray will turn green.  
4. You can now minimize the application window. It will continue running in the background.

## **Running as a Windows Service (Advanced)**

For a truly "set it and forget it" solution, you can run the eGPU Manager as a Windows Service. This ensures it starts automatically with your computer and runs in the background without needing a user to be logged in. The easiest way to do this is with a tool called **NSSM (the Non-Sucking Service Manager)**.

   With version II, you can install/unsinstall/start/stop the service eGPUManager from tray menu.

   Also, you can manage it from the Windows Services panel (services.msc). If activate, the service will now start automatically every time you boot your computer.

## **Troubleshooting**

### **Compilation Error: "'Properties' does not exist in the current context"**

If you see this error in Visual Studio's "Error List" when trying to build the project, it means the application's settings file hasn't been created yet.

**Solution:**

1. In the **Solution Explorer** panel on the right, find and double-click the **"Properties"** folder.  
2. Go to the **"Settings"** tab.  
3. You will see a message like "This project does not contain a default settings file. Click here to create one." **Click that link.**  
4. A grid will appear. Add a new setting with the following values:  
   * **Name**: DeviceInstanceId (must be exact)  
   * **Type**: string  
   * **Scope**: User  
   * **Value**: (leave blank)  
5. Press Ctrl \+ S to save the file.  
6. Build the project again (F6). The error should now be gone.

### **"Error 43" in Device Manager**

This tool is designed to prevent crashes during sleep/lock transitions. It is **not** a fix for an initial "Error 43" that appears when you first connect the eGPU. If your eGPU is not working correctly from the start, you must resolve that issue first. Common solutions include:

* Using Display Driver Uninstaller (DDU) to perform a clean reinstall of your GPU drivers.  
* Seeking specific workarounds for your laptop/eGPU model on communities like [egpu.io](https://egpu.io/).

Once your eGPU is recognized and works correctly, this tool will help maintain its stability.

## **How It Works**

The application uses the Microsoft.Win32.SystemEvents class in .NET to subscribe to native Windows system events:

* PowerModeChanged: Detects when the system is about to suspend (PowerModes.Suspend) or has just resumed (PowerModes.Resume).  
* SessionSwitch: Detects when the user session is locked (SessionSwitchReason.SessionLock) or unlocked (SessionSwitchReason.SessionUnlock).

When one of these events is triggered, the application calls the low-level Windows SetupAPI functions (via P/Invoke) to programmatically disable or enable the hardware device matching the provided InstanceId. This ensures the device is in a safe state before the transition occurs.