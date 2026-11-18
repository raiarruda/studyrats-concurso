/**
 * Fotos de celular passam fácil de 5 MB. Reduz para no máximo `maxSide` px e reexporta em JPEG,
 * o que mantém o upload rápido e abaixo do limite do servidor. Se algo falhar, devolve o original.
 */
export async function resizeImage(file: File, maxSide = 1600, quality = 0.85): Promise<Blob> {
  try {
    const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' })
    const scale = Math.min(1, maxSide / Math.max(bitmap.width, bitmap.height))
    const canvas = document.createElement('canvas')
    canvas.width = Math.round(bitmap.width * scale)
    canvas.height = Math.round(bitmap.height * scale)
    canvas.getContext('2d')!.drawImage(bitmap, 0, 0, canvas.width, canvas.height)
    bitmap.close()

    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', quality))
    return blob && blob.size < file.size ? blob : file
  } catch {
    return file
  }
}
