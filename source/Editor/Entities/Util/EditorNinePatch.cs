using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.Editor.Entities.Util;

public class EditorNinePatch {

    private MTexture[,] nineSliceTexture;

    public EditorNinePatch(MTexture mtexture) {

        int tileWidth = mtexture.Width/8;
        int tileHeight = mtexture.Height/8;

        nineSliceTexture = new MTexture[tileWidth, tileHeight];

        for (int i = 0; i < 2; i++) {
            for (int j = 0; j < 2; j++) {
                int x = (i == 1 ? tileWidth - 1 : i);
                int y = (j == 1 ? tileHeight - 1 : j);
                nineSliceTexture[x, y] = mtexture.GetSubtexture(new Rectangle(x * 8, y * 8, 8, 8));
            }
        }
        for (int i = 1; i < tileWidth - 1; i++) {
            nineSliceTexture[i, 0] = mtexture.GetSubtexture(new Rectangle(i*8, 0, 8, 8));
            nineSliceTexture[i, tileHeight - 1] = mtexture.GetSubtexture(new Rectangle(i*8, (tileHeight - 1) * 8, 8, 8));
        }
        for (int j = 1; j < tileHeight - 1; j++) {
            nineSliceTexture[0, j] = mtexture.GetSubtexture(new Rectangle(0, j*8, 8, 8));
            nineSliceTexture[tileWidth - 1, j] = mtexture.GetSubtexture(new Rectangle((tileWidth - 1) * 8, j * 8, 8, 8));
        }
        for (int k = 1; k < tileWidth - 1; k++) {
            for (int l = 1; l < tileHeight - 1; l++) {
                nineSliceTexture[k, l] = mtexture.GetSubtexture(new Rectangle(k*8, l * 8, 8, 8));
            }
        }
    }

    public void Draw(Vector2 pos, int width, int height, Color color) {
        int tileWidth = (int)(width / 8f);
        int tileHeight = (int)(height / 8f);

        int rows = nineSliceTexture.GetLength(0);
        int columns = nineSliceTexture.GetLength(1);

        // nr => 1 - max (if nr >= 0)
        static int moduloClamp(int nr, int max) {
            return (nr % max) + 1;
        };

        nineSliceTexture[0, 0].Draw(pos + new Vector2(0f, 0f), Vector2.Zero, color);
        nineSliceTexture[rows - 1, 0].Draw(pos + new Vector2(width - 8f, 0f), Vector2.Zero, color);
        nineSliceTexture[0, columns - 1].Draw(pos + new Vector2(0f, height - 8f), Vector2.Zero, color);
        nineSliceTexture[rows - 1, columns - 1].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, color);

        for (int i = 1; i < tileWidth - 1; i++) {
            nineSliceTexture[moduloClamp(i - 1, rows - 2), 0].Draw(pos + new Vector2((float)(i * 8), 0f), Vector2.Zero, color);
            nineSliceTexture[moduloClamp(i - 1, rows - 2), columns - 1].Draw(pos + new Vector2((float)(i * 8), height - 8f), Vector2.Zero, color);
        }

        for (int j = 1; j < tileHeight - 1; j++) {
            nineSliceTexture[0, moduloClamp(j - 1, columns - 2)].Draw(pos + new Vector2(0f, (float)(j * 8)), Vector2.Zero, color);
            nineSliceTexture[rows - 1, moduloClamp(j - 1, columns - 2)].Draw(pos + new Vector2(width - 8f, (float)(j * 8)), Vector2.Zero, color);
        }

        for (int k = 1; k < tileWidth - 1; k++) {
            for (int l = 1; l < tileHeight - 1; l++) {
                nineSliceTexture[moduloClamp(k - 1, rows - 2), moduloClamp(l - 1, rows - 2)].Draw(pos + new Vector2((float)k, (float)l) * 8f, Vector2.Zero, color);
            }
        }
    }
}