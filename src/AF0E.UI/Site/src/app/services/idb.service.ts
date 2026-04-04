import {Injectable} from '@angular/core';
import {from, Observable} from 'rxjs';

const DB_NAME = 'af0e';
const DB_VERSION = 1;
const STORE_NAME = 'logbook';

@Injectable({providedIn: 'root'})
export class IdbService {

  private _db: IDBDatabase | null = null;

  private open(): Promise<IDBDatabase> {
    if (this._db) return Promise.resolve(this._db);

    return new Promise<IDBDatabase>((resolve, reject) => {
      const request = indexedDB.open(DB_NAME, DB_VERSION);

      request.onupgradeneeded = (event) => {
        const db = (event.target as IDBOpenDBRequest).result;
        if (!db.objectStoreNames.contains(STORE_NAME)) {
          const store = db.createObjectStore(STORE_NAME, {keyPath: 'id', autoIncrement: true});
          store.createIndex('call', 'call', {unique: false});
          store.createIndex('date', 'date', {unique: false});
          store.createIndex('activation', 'activationId', {unique: false});
        }
      };

      request.onsuccess = (event) => {
        this._db = (event.target as IDBOpenDBRequest).result;
        resolve(this._db);
      };

      request.onerror = (event) => {
        reject((event.target as IDBOpenDBRequest).error);
      };
    });
  }

  saveQso(activationId: number | null, qso: any): Observable<IDBValidKey> {
    return from(
      this.open().then(
        db => new Promise<IDBValidKey>((resolve, reject) => {
          const tx = db.transaction(STORE_NAME, 'readwrite');
          const store = tx.objectStore(STORE_NAME);
          // put() will insert or update based on keyPath (id)
          const request = store.put({...qso, activationId, _savedAt: new Date().toISOString()});
          request.onsuccess = () => resolve(request.result);
          request.onerror  = () => reject(request.error);
        })
      )
    );
  }

  getAllQsos(): Observable<any[]> {
    return from(
      this.open().then(
        db => new Promise<any[]>((resolve, reject) => {
          const tx = db.transaction(STORE_NAME, 'readonly');
          const store = tx.objectStore(STORE_NAME);
          const request = store.getAll();
          request.onsuccess = () => resolve((request.result as any[]).reverse());
          request.onerror  = () => reject(request.error);
        })
      )
    );
  }

  getActivationIds(): Observable<number[]> {
    return from(
      this.open().then(
        db => new Promise<number[]>((resolve, reject) => {
          const tx = db.transaction(STORE_NAME, 'readonly');
          const index = tx.objectStore(STORE_NAME).index('activation');
          // 'prevunique' walks the index in descending order, skipping duplicate keys
          const request = index.openKeyCursor(IDBKeyRange.lowerBound(0, true), 'prevunique');
          const ids: number[] = [];
          request.onsuccess = (event) => {
            const cursor = (event.target as IDBRequest<IDBCursor>).result;
            if (cursor) {
              ids.push(cursor.key as number);
              cursor.continue();
            }
          };
          tx.oncomplete = () => resolve(ids);
          tx.onerror    = () => reject(tx.error);
        })
      )
    );
  }

  deleteQso(id: number): Observable<void> {
    return from(
      this.open().then(
        db => new Promise<void>((resolve, reject) => {
          const tx = db.transaction(STORE_NAME, 'readwrite');
          const store = tx.objectStore(STORE_NAME);
          const request = store.delete(id);
          request.onsuccess = () => resolve();
          request.onerror  = () => reject(request.error);
        })
      )
    );
  }

  deleteQsosByActivationId(activationId: number): Observable<void> {
    return from(
      this.open().then(
        db => new Promise<void>((resolve, reject) => {
          const tx = db.transaction(STORE_NAME, 'readwrite');
          const store = tx.objectStore(STORE_NAME);
          const index = store.index('activation');
          const request = index.openCursor(IDBKeyRange.only(activationId));
          request.onsuccess = (event) => {
            const cursor = (event.target as IDBRequest<IDBCursorWithValue>).result;
            if (cursor) {
              cursor.delete();
              cursor.continue();
            }
          };
          tx.oncomplete = () => resolve();
          tx.onerror    = () => reject(tx.error);
        })
      )
    );
  }

  deleteAllQsos(): Observable<void> {
    return from(
      this.open().then(
        db => new Promise<void>((resolve, reject) => {
          const tx = db.transaction(STORE_NAME, 'readwrite');
          const store = tx.objectStore(STORE_NAME);
          const request = store.clear();
          request.onsuccess = () => resolve();
          request.onerror  = () => reject(request.error);
        })
      )
    );
  }
}
