# Генерационный промпт для маскота Лекси

> Готовый промпт для Midjourney / DALL-E / GPT-image / Nano Banana / Stable Diffusion.
> Источник требований: `lexify-mascot.md`.

## Как пользоваться

1. Сначала сгенерируйте **character sheet** (промпт A) — это эталон персонажа.
2. Затем генерируйте позы по одной (промпт B + строка нужной позы), прикладывая эталон как референс (image prompt / "use this character"), чтобы персонаж не «плыл» между картинками.
3. Негативный промпт (C) добавляйте всегда, если генератор его поддерживает.
4. Результат: PNG 1024×1024 на прозрачном фоне → векторизация в SVG вручную или через трассировку.

---

## A. Character sheet (эталон)

```
Character reference sheet of "Lexi", a cute pink axolotl mascot for a
vocabulary learning app, in classic kawaii sticker style. Front view, side
view and 3/4 view of the same character.

CHARACTER: a chubby pastel-pink axolotl with an oversized round head (head
is ~55% of total height), a plump rounded body, tiny stubby paws and a
thick curled pink tail with a darker inner fin. Three pairs of external
gills on the sides of the head shaped like fluffy branching fern leaves /
coral fronds (NOT simple stalks with balls) — the gills are the key
recognizable feature. Small black oval eyes, each with one or two white
highlight dots (a bigger one top-left and a tiny one bottom-right) that
make the eyes look glossy and alive; a tiny kawaii ":3" cat-like mouth
(small w-shaped smile), big soft round pink blush on the cheeks. Sweet,
calm, friendly expression.

COLORS (exact hex, flat fills only):
- body, head, paws: #FBD9E0 (pastel pink)
- gills, tail inner fin: #F096AC (deeper rose pink)
- cheek blush: #F6B8C6
- subtle shading patches (belly/paw creases): #F4C3CF
- eyes and mouth: #2B2226 (soft black); eye highlights: #FFFFFF
- outline: #2B2226, bold and uniform
- decorative bubbles: #CDEBF5 (light blue)

STYLE: flat 2D kawaii sticker illustration with a BOLD dark outline of
uniform thickness around the whole character and its inner details, clean
solid fills, at most 1–2 flat shading tones, no gradients, no realistic
shadows, no texture. Chibi proportions, rounded everything. The silhouette
must stay readable when scaled down to a 16px icon.

FORMAT: transparent background, 1024x1024, centered, generous margins.
(For sticker exports only: add a thick white die-cut sticker border; for
in-app assets — no white border.)
```

## B. Базовый блок для каждой позы

```
"Lexi" the axolotl mascot — same character as the reference sheet: chubby
pastel-pink (#FBD9E0) axolotl with an oversized round head, three pairs of
fluffy fern-leaf gills in rose pink (#F096AC), small black oval eyes with
white highlight dots (glossy look), tiny kawaii ":3" mouth, round pink
blush, thick curled tail, bold uniform
dark (#2B2226) outline. Flat 2D kawaii sticker style, solid fills, no
gradients, no realistic shadows, transparent background, 1024x1024,
centered, full body visible.

POSE:
```

…и после `POSE:` подставляйте одну из строк:

| # | Поза (id в коде) | Строка для промпта |
|---|---|---|
| 1 | `greeting` | waving hello with one raised front paw, warm open smile, head tilted slightly, welcoming and friendly |
| 2 | `celebrate` | jumping with joy, both paws raised up, eyes closed in happy arches (^ ^), open cheering mouth, colorful confetti dots (#FB923C, #4ADE80, #FBBF24, #60A5FA) floating around |
| 3 | `sleep` | sleeping peacefully, eyes closed as gentle downward arcs, tiny round open mouth, three light-blue bubbles (#93C5FD) rising above its head, body slightly curled, serene |
| 4 | `confused` | puzzled and slightly embarrassed, scratching the back of its head with one paw, gills drooping downward, wavy uncertain mouth, one eyebrow-like eye squint, small question mark floating nearby |
| 5 | `diving` | swimming/diving pose seen slightly from the side, body tilted forward, holding a small butterfly net, trail of small bubbles behind, focused cheerful expression |
| 6 | `lost` | standing and holding an unfolded paper map with both paws, looking around confusedly, gills slightly droopy, small sweat drop near the head |
| 7 | `builder` | wearing a yellow hard hat, holding a wrench in one paw, determined helpful smile, sleeves-rolled-up energy |
| 8 | `scientist` | wearing round glasses, holding a tiny open dictionary/book with both paws, thoughtful smart expression, one gill bent like a raised eyebrow |
| 9 | `pointing` | encouraging pose, pointing forward/right with one paw, inviting smile, other paw on hip, "come on, let's go" energy |

## C. Негативный промпт

```
blue body, realistic, 3D render, gradients, heavy shadows, fur, teeth,
human hands, text, watermark, background scenery, gray colors, dull
colors, extra limbs, empty dead eyes without highlights, more or fewer
than 6 gills, simple ball-tipped gill stalks, sad or crying expression,
aggressive expression
```

## D. Дополнительно: иконка

```
App icon of "Lexi" the axolotl mascot: HEAD ONLY, front view, filling ~80%
of the canvas. Same character: pastel-pink #FBD9E0 head, three pairs of
fluffy fern-leaf gills in rose pink #F096AC spread symmetrically left and
right, black oval eyes with white highlight dots, tiny ":3" mouth, round
pink blush, bold dark #2B2226 outline. Flat kawaii sticker style, solid
fills, no gradients, transparent background, 1024x1024. The silhouette
must read clearly at 16x16 pixels.
```

## E. Анимации (image-to-video: Kling / Runway / Pika / Luma)

Входные картинки — `Info/mascot-animation/*-green.png` (позы на хромакей-зелёном
`#00B140`; сервисы затирают прозрачность, поэтому фон запечён заранее).

Общие настройки: длительность 5 с, соотношение 1:1, motion strength / intensity —
низкая (2–4 из 10), режим loop если есть (Luma). В Runway — motion brush: закрасить
только то, что должно двигаться (жабры, пузыри, конфетти).

### E1. `celebrate-green.png` — праздник (одноразовое проигрывание)

```
Flat 2D cartoon sticker animation. The cute pink axolotl bounces up and down
joyfully in place, both paws raised, cheering; the colorful confetti pieces
around it flutter and slowly fall; the fern-like gill fronds bounce softly
with the motion. The character stays centered and keeps its exact flat
cartoon style and proportions. Static camera, no zoom, no pan, the solid
green background stays completely unchanged.
```

### E2. `sleep-green.png` — сон (бесшовный луп)

```
Flat 2D cartoon sticker animation. The sleeping pink axolotl breathes
slowly and gently — its round body rises and falls very subtly; the three
light-blue bubbles above its head drift slowly upward and are replaced by
new ones; the gill fronds sway very slightly. Calm, minimal, cozy motion,
seamless loop. Static camera, no zoom, character stays in place, exact flat
cartoon style preserved, solid green background completely unchanged.
```

### E3. `diving-green.png` — плавание (бесшовный луп)

```
Flat 2D cartoon sticker animation. The pink axolotl swims gently in place,
holding its small butterfly net; the tail sways slowly side to side, the
fern-like gill fronds ripple as if in a light water current, the small
bubbles drift slowly past it. Smooth, calm, looping swimming motion. Static
camera, no zoom, character stays centered, exact flat cartoon style
preserved, solid green background completely unchanged.
```

### E4. Негативный промпт для анимаций

```
camera movement, zoom, pan, parallax, 3D, realistic, style change,
background change, background movement, character morphing, melting,
distortion, extra limbs, extra eyes, face change, text, watermark,
fast motion, jerky motion
```

### E5. Постобработка (после генерации)

1. Вырезать зелёный фон: unscreen.com, либо ffmpeg:
   `ffmpeg -i in.mp4 -vf "colorkey=0x00B140:0.28:0.06,despill=type=green" -c:v libvpx-vp9 -pix_fmt yuva420p -b:v 0 -crf 32 -an out.webm`
2. Проверить луп (sleep/diving): при необходимости `-vf reverse` + concat («пинг-понг»).
3. Итоговые файлы: `frontend/src/shared/ui/mascot/poses/{pose}.webm` (512×512, ≤300 КБ).
4. Приёмка: контур без зелёного ореола; стиль/пропорции не «поплыли»; движение мягкое.

## Чек-лист приёмки каждой картинки

- [ ] Тело пастельно-розовое `#FBD9E0`, жабры насыщённее (`#F096AC`)
- [ ] Ровно 3 пары жабр в форме пышных «папоротниковых» листьев (не палочки с шариками)
- [ ] Глаза чёрные овалы **с белыми бликами** (глянцевый «живой» взгляд, как на референсе)
- [ ] Рот — маленькое кавайное «:3»
- [ ] Жирный равномерный тёмный контур `#2B2226` по всему персонажу
- [ ] Плоская заливка: без градиентов и реалистичных теней (допустимы 1–2 плоских тона)
- [ ] Прозрачный фон, персонаж целиком в кадре с полями
- [ ] Силуэт читается при уменьшении до 32 px
- [ ] Эмоция соответствует позе из таблицы; ничего пугающего/грустного
