# TactileCs

C# / .NET 8 port of the core isohedral tiling geometry from Craig S. Kaplan’s
[Tactile](https://github.com/isohedral/tactile) library.

This project focuses on the **geometry and tiling engine**, not rendering. It exposes:

- `TactileCs.Geometry.Vector2`, `Transform2D`, `Polygon`
- `TactileCs.Tiling.SymmetryGroup`, `EdgeShape`
- `TactileCs.Tiling.IsohedralTiling` – a table-driven wrapper for isohedral tiling types

## Status

- Skeleton implementation with a minimal type definition (Type 1 / parallelogram) and the correct API surface.
- Ready to extend by porting the original tiling-type tables and per-type vertex equations from Tactile.

## License

This is a derivative work of:

> Tactile – Isohedral tilings and decorated tilings  
> Copyright (c) 2018, Craig S. Kaplan

used under the **BSD 3-Clause License**. See `LICENSE` for full terms.
