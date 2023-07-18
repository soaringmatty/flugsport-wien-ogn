import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OrientationNotSupportedComponent } from './orientation-not-supported.component';

describe('OrientationNotSupportedComponent', () => {
  let component: OrientationNotSupportedComponent;
  let fixture: ComponentFixture<OrientationNotSupportedComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [OrientationNotSupportedComponent]
    });
    fixture = TestBed.createComponent(OrientationNotSupportedComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
