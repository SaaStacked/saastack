export function randomComponentId() {
  return Math.random().toString(36).substring(7);
}

export function createComponentId(suffix: string, id?: string) {
  return id ? `${id}_${suffix}` : `${suffix}(${randomComponentId()})`;
}

export function toClasses(styles: (string | undefined)[]) {
  return styles.filter(Boolean).join(' ');
}
