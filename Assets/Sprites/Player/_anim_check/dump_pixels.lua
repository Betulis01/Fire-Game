-- Dumps a compact colored-grid view of specific frames/layers to find exact pixel data.
local spr = app.sprite
if not spr then
  print("NO SPRITE")
  return
end

local function colorKey(c)
  if c.alpha == 0 then return "." end
  return string.format("%02x%02x%02x%02x", c.red, c.green, c.blue, c.alpha)
end

local function layerByName(name)
  for _, l in ipairs(spr.layers) do
    if l.name == name then return l end
  end
  return nil
end

local function dumpFrame(frameIndex, layerName, x0, y0, x1, y1)
  local layer = layerByName(layerName)
  print(string.format("=== frame %d layer %s ===", frameIndex, layerName))
  local cel = layer:cel(frameIndex + 1)
  if not cel then
    print("  (no cel)")
    return
  end
  local img = cel.image
  local cx, cy = cel.position.x, cel.position.y
  for y = y0, y1 do
    local row = {}
    for x = x0, x1 do
      local lx, ly = x - cx, y - cy
      local c
      if lx >= 0 and ly >= 0 and lx < img.width and ly < img.height then
        c = img:getPixel(lx, ly)
        c = Color(c)
      else
        c = Color(0,0,0,0)
      end
      table.insert(row, colorKey(c))
    end
    print(y .. ": " .. table.concat(row, " "))
  end
end

-- Frames of interest: se_idle=0, ne_idle=12, se_attack_r=40-42, se_attack_l=43-45, ne_attack_l=46-48
local frames = {0, 12, 40,41,42, 43,44,45, 46,47,48}
for _, f in ipairs(frames) do
  dumpFrame(f, "Body", 18, 10, 46, 48)
end
