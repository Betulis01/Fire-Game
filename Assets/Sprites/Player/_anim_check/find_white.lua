local spr = app.sprite
for _, layer in ipairs(spr.layers) do
  for f = 40, 51 do
    local cel = layer:cel(f + 1)
    if cel then
      local img = cel.image
      local cx, cy = cel.position.x, cel.position.y
      for y = 0, img.height - 1 do
        for x = 0, img.width - 1 do
          local c = Color(img:getPixel(x, y))
          if c.alpha > 0 and c.red > 220 and c.green > 220 and c.blue > 220 then
            print(string.format("frame=%d layer=%s local=(%d,%d) canvas=(%d,%d) color=%02x%02x%02x%02x opacity=%d blend=%s",
              f, layer.name, x, y, cx + x, cy + y, c.red, c.green, c.blue, c.alpha, cel.opacity, tostring(layer.blendMode)))
          end
        end
      end
    end
  end
end
print("DONE")
