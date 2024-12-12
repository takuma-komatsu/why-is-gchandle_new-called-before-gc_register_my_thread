# why-is-gchandle_new-called-before-gc_register_my_thread
Example project for reporting a bug where GCHandle::New() is called before gc_register_my_thread() on a background thread when using AssetBundle.LoadFromStreamAsync.
