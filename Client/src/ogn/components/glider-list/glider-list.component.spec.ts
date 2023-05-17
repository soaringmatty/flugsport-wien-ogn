import { ComponentFixture, TestBed } from '@angular/core/testing';

import GliderListComponent from './glider-list.component';

describe('GliderListComponent', () => {
  let component: GliderListComponent;
  let fixture: ComponentFixture<GliderListComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [GliderListComponent]
    });
    fixture = TestBed.createComponent(GliderListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
