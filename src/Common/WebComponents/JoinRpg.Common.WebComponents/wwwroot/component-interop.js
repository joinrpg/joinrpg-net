export function showModal(dialog) {
  try {
    dialog.showModal();
  } catch (e) {
    console.error('Ошибка при открытии:', e);
  }
}

export function closeModal(dialog) {
  dialog.close();
}
