# why-is-gchandle_new-called-before-gc_register_my_thread

Example project for reporting a bug where GCHandle::New() is called before GC_register_my_thread() on a background thread when using AssetBundle.LoadFromStreamAsync().

## Bug Details

When you call AssetBundle.LoadFromStreamAsync(), Unity will start the load process in a background thread.
At this time, if the following conditions are met, your application will crash with [this ABORT()](https://github.com/ivmai/bdwgc/blob/f3d2d4a45c423d9bbe78b8670f2a1469cdd79ebc/pthread_stop_world.c#L940), regardless of platform:

- Condition 1
  - When the assigned thread is used without being registered as a managed thread (i.e., the first time it is used)
- Condition 2
  - When Incremental GC is performed in GCHandle::New() called from AssetBundleLoadFromManagedStreamAsyncOperation::LoadArchiveJob()

The cause of the crash is indicated by the ABORT() message "Collecting from unknown thread", which specifically means that GC_register_my_thread() has not been called on that thread.
So calling GC_register_my_thread() before the first call to GCHandle::New() in each background thread should solve this problem.
(Alternatively, you may be able to work around this by disabling GC until you call GC_register_my_thread().)

Either way, I think fixing this bug would require a change to the engine code.

## Regarding debug code added to IL2CPP

I added the following debug code to set a breakpoint when GCHandle::New is called before GC_register_my_thread() is called.
Also, because this bug has a very low occurrence rate, we have provided code to reproduce the situation.

https://github.com/takuma-komatsu/why-is-gchandle_new-called-before-gc_register_my_thread/blob/1c3c1994a7ea8754991bcfd23f7b7aead732d8f2/il2cpp-patch/libil2cpp/gc/GCHandle.cpp#L60-L75

NOTE: During the build pre-processing, IL2CPP is copied locally from the Unity installation folder, the above patch is applied, and then the build is performed using that.

### ⚠WARNING⚠

**If you wish to modify the IL2CPP runtime code and release your product, you will need to purchase a “Source Code Adapt” subscription to Unity.**

https://unity.com/products/source-code

## How to build && debug

### Windows

1. Open "Build Settings"
1. Check "Create Visual Studio Solution".
1. Click the "Build" button.
1. Open the generated solution in Visual Studio.
1. Open \<OutputDir\>/Il2CppOutputProject/IL2CPP/libil2cpp/gc/GCHandle.cpp.
1. Search for the string "IL2CPP_PATCH" and set a breakpoint.
1. Start debugging.

If you uncomment GC_start_incremental_collection() it crashes with this stack trace:

![image](https://github.com/user-attachments/assets/d424480f-c9ff-44d9-826e-276461afc9db)

**This stack trace is almost identical to what you would see if a real bug occurred.**

### Mac

1. Open "Build Settings"
1. Check "Create Xcode Project".
1. Click the "Build" button.
1. Open the generated .xcodeproj in Xcode.
1. Open \<OutputDir\>/Il2CppOutputProject/IL2CPP/libil2cpp/gc/GCHandle.cpp.
1. Search for the string "IL2CPP_PATCH" and set a breakpoint.
1. Start debugging.

If you uncomment GC_start_incremental_collection(), it crashes just like on Windows.
