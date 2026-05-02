const sharp = require('sharp');
const fs = require('fs');
const path = require('path');

const dir = __dirname;
const files = fs.readdirSync(dir).filter(f => /\.(jpg|jpeg|png)$/i.test(f));

console.log(`Converting ${files.length} images to WebP...`);

let converted = 0;
let failed = 0;

files.forEach(file => {
  const inputPath = path.join(dir, file);
  const outputPath = path.join(dir, path.parse(file).name + '.webp');
  
  sharp(inputPath)
    .webp({ quality: 82 })
    .toFile(outputPath, (err, info) => {
      if (err) {
        console.error(`✗ Failed: ${file}`, err.message);
        failed++;
      } else {
        converted++;
        const inputSize = fs.statSync(inputPath).size;
        const outputSize = fs.statSync(outputPath).size;
        const savings = ((1 - outputSize / inputSize) * 100).toFixed(1);
        console.log(`✓ ${file} → ${path.basename(outputPath)} (${savings}% smaller)`);
      }
      
      if (converted + failed === files.length) {
        console.log(`\nDone! Converted: ${converted}, Failed: ${failed}`);
      }
    });
});
