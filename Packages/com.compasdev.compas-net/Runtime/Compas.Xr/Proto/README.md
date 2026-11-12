# Generated COMPAS XR bindings

`CompasXr.cs` is generated from `compas_xr.proto`, which is owned by
[compas_xr](https://github.com/compas-dev/compas_xr). It is **not** written by hand.

## Status: placeholder

COMPAS XR publishes the `.proto` bundle and the generated bindings for each supported
language as release assets, and consumers pin a released version — see the
[compas_pb architecture notes](https://github.com/compas-dev/compas_pb/blob/main/docs/architecture.md#distributing-schemas).
That is what `fetch_compas_xr.py` in the repository root does.

No compas_xr release carries those assets yet. Until one does, the file here was generated
locally from the compas_xr working tree:

```bash
cd /path/to/compas_xr
invoke generate-proto-classes --target-language=csharp
cp dist/generated/csharp/CompasXr.cs /path/to/compas-net/src/Runtime/Compas.Xr/Proto/
```

## Switching to the released bindings

Once compas_xr ships a release with the binding assets:

1. Set `COMPAS_XR_VERSION` and `CSHARP_ASSET_SHA256` in `fetch_compas_xr.py`.
2. Run `python3 fetch_compas_xr.py`, which overwrites this file from the pinned release.
3. Stop committing the generated file — gitignore it and fetch it in CI before building,
   the way compas_pb_csharp treats the COMPAS core schemas.

Until then, treat a change here as a wire change: it must correspond to a `compas_xr.proto`
change on the compas_xr side, never a local edit.

## Making the conversions available at runtime

.NET loads a referenced assembly lazily, on first use of one of its types. CompasPb
discovers registrars by sweeping loaded assemblies, so a host that receives a message
before it has constructed any XR type has this assembly unloaded at that moment and
`Unpack` fails with `No deserializer is registered for protobuf type 'compas_xr.v1....'`.

That is the normal shape of an MQTT subscriber. Call `Compas.Xr.CompasXr.Initialize()`
once during start-up. In Unity, call it from a `[RuntimeInitializeOnLoadMethod]` in the
package that owns the XR scene code — this assembly carries no UnityEngine reference by
design.
