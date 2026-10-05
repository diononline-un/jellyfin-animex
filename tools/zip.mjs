// Packt einen Ordner als ZIP. Ersatz für das zip-Programm, das im Node-Container fehlt.
// Aufruf: node zip.mjs <quellordner> <zieldatei>
//
// Archiver liegt in <repo>/.ziptool und wird beim Bau dort installiert. Fassung 8 stellt
// Klassen bereit (Archiver, ZipArchive), ältere Fassungen eine Fabrikfunktion. Beide Formen
// werden bedient, damit ein Wechsel der Fassung den Bau nicht bricht.
import { createWriteStream } from 'node:fs';
import { mkdir } from 'node:fs/promises';
import { dirname } from 'node:path';
import { createRequire } from 'node:module';

const [source, target] = process.argv.slice(2);
if (!source || !target) {
  console.error('Aufruf: node zip.mjs <quellordner> <zieldatei>');
  process.exit(2);
}

const require = createRequire(import.meta.url);
const loaded = require(process.env.ARCHIVER_PATH ?? '/src/.ziptool/node_modules/archiver');

function createArchive() {
  const options = { zlib: { level: 9 } };
  if (typeof loaded === 'function') {
    return loaded('zip', options);
  }

  const ZipArchive = loaded.ZipArchive ?? loaded.default?.ZipArchive;
  if (typeof ZipArchive === 'function') {
    return new ZipArchive(options);
  }

  const Archiver = loaded.Archiver ?? loaded.default?.Archiver;
  if (typeof Archiver === 'function') {
    return new Archiver('zip', options);
  }

  console.error('Archiver-Fassung nicht unterstützt. Vorhandene Schlüssel: ' + Object.keys(loaded).join(', '));
  process.exit(3);
}

await mkdir(dirname(target), { recursive: true });
const output = createWriteStream(target);
const archive = createArchive();

const done = new Promise((resolve, reject) => {
  output.on('close', () => resolve(archive.pointer()));
  archive.on('error', reject);
});

archive.pipe(output);
archive.directory(source, false);
await archive.finalize();
const bytes = await done;
console.log(`ZIP geschrieben: ${target} (${bytes} Bytes)`);
