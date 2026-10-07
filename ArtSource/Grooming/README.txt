Portrait grooming art — 2026-10-07

Generated using the built-in image_gen tool, with the user's Taiko Risshiden V screenshot as a painting-style reference and the existing portrait atlases as pose references. These are original replacement assets.

hair-master.png: 1024x1536 RGBA, 4 columns x 4 rows, nominal 256x384 cells. Male hairstyles occupy the first two rows; female hairstyles the last two rows. Eight styles per sex. A second image_gen pass added transparent spacing around each silhouette. Runtime per-style source bounds preserve long tails without sampling neighbouring art.
beard-master.png: 1448x1086 RGBA, 4 columns x 2 rows, 362x543 cells. Seven beard styles plus one transparent unused cell. UI index 0 means no beard, indices 1–7 use cells 0–6.

Runtime copies: Assets/Minglu/Resources/Minglu/Art/Painted/hair-styles.png and beard-styles.png.
The original generated alpha is preserved. No external source-game art was extracted or included. Registration is measured against the actual generated cells and each of the sixteen existing face paintings.
Exact prompts are recorded in generation-prompts.txt.

Hair order, male: topknot / short tied / loose shoulder hair / cloth scholar cap / low bun / scholar knot / half-up / bound tail.
Hair order, female: high bun / low bun / side bun / half-up / double-loop bun / braided bun / low loop / simple pin.
Beard order (UI 1-7): short goatee / long scholar beard / natural downward moustache / close full beard / pointed goatee / three-part beard / short moustache.
