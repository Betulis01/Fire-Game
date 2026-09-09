local spr = app.sprite
local function layerByName(name)
  for _, l in ipairs(spr.layers) do
    if l.name == name then return l end
  end
end
local bodyL = layerByName("Body")

local function getCanvasPixel(cel, x, y)
  if not cel then return Color(0,0,0,0) end
  local img = cel.image
  local lx, ly = x - cel.position.x, y - cel.position.y
  if lx < 0 or ly < 0 or lx >= img.width or ly >= img.height then
    return Color(0,0,0,0)
  end
  return Color(img:getPixel(lx, ly))
end

-- anchor near neck/shoulder
local ax, ay = 32, 39

for f = 40, 51 do
  local cel = bodyL:cel(f + 1)
  local bestX, bestY, bestD = nil, nil, -1
  for y = 36, 44 do
    for x = 15, 50 do
      local c = getCanvasPixel(cel, x, y)
      if c.alpha > 0 then
        local d = (x-ax)*(x-ax) + (y-ay)*(y-ay)
        if d > bestD then bestD = d; bestX = x; bestY = y end
      end
    end
  end
  print(string.format("frame=%d hand=(%d,%d) dist=%.1f", f, bestX or -1, bestY or -1, math.sqrt(bestD)))
end
